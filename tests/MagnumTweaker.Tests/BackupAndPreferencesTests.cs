namespace MagnumTweaker.Tests;

public sealed class BackupAndPreferencesTests
{
    [Theory]
    [InlineData(true, "original", "String")]
    [InlineData(false, null, null)]
    public void BackupRetainsTheFirstSnapshotAcrossReloads(bool existed, string? value, string? kind)
    {
        using var workspace = new TemporaryWorkspace();
        var store = workspace.Backups;
        store.Save("setting", value, kind, existed);
        store.Save("setting", "second", "String", true);

        Assert.Equal(new BackupValue(value, kind, existed), workspace.Backups.Get("setting"));
        Assert.Single(Directory.GetFiles(workspace.Root));
    }

    [Fact]
    public void ForgetOnlyRemovesTheRestoredSetting()
    {
        using var workspace = new TemporaryWorkspace();
        var store = workspace.Backups;
        store.Save("first", "one", "String", true);
        store.Save("second", "two", "String", true);
        store.Forget("first");
        store.Forget("missing");

        var reloaded = workspace.Backups;
        Assert.False(reloaded.Contains("first"));
        Assert.Equal("two", reloaded.Get("second")?.Value);
    }

    [Theory]
    [InlineData("{broken")]
    [InlineData("null")]
    public void CorruptBackupPreventsWritesAndPreservesTheSource(string original)
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("backup.json");
        File.WriteAllText(path, original);

        Assert.Throws<IOException>(() => workspace.Backups.Save("key", "value", "String", true));
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(62, 62)]
    [InlineData(500, 100)]
    public void PreferencesRoundTripAndClampVolume(int volume, int expected)
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath(@"nested\interface.json");
        var preferences = new InterfacePreferences { English = true, WindowsProfile = "11", HoverVolume = volume, AcceptedTermsVersion = 1 };
        preferences.Save(path);

        var loaded = InterfacePreferences.Load(path);
        Assert.Equal(expected, loaded.HoverVolume);
        Assert.True(loaded.English);
        Assert.Equal("11", loaded.WindowsProfile);
        Assert.Equal(1, loaded.AcceptedTermsVersion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{broken")]
    public void PreferencesLoadDefaultsForMissingOrCorruptFiles(string? contents)
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("interface.json");
        if (contents != null) File.WriteAllText(path, contents);

        var loaded = InterfacePreferences.Load(path);
        Assert.Equal(10, loaded.HoverVolume);
        Assert.Null(loaded.WindowsProfile);
        Assert.Equal(0, loaded.AcceptedTermsVersion);
    }
}
