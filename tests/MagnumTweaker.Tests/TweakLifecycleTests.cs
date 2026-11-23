using Microsoft.Win32;

namespace MagnumTweaker.Tests;

public sealed class TweakLifecycleTests
{
    [Theory]
    [InlineData("original", 1)]
    [InlineData("desired", 0)]
    public async Task StoredTweakAppliesOnlyWhenNeededAndRestoresOriginal(string original, int expectedWrites)
    {
        using var workspace = new TemporaryWorkspace();
        var store = workspace.Backups;
        string current = original;
        int writes = 0;
        var tweak = AdditionalTweaks.Stored("test", "Title", "Title", "Description", "Description", "desired",
            _ => Task.FromResult(current), (value, _) => { current = value; writes++; return Task.CompletedTask; }, store);

        await tweak.Apply(default);
        await tweak.Apply(default);

        Assert.Equal(expectedWrites, writes);
        Assert.Equal(TweakState.Applied, await tweak.Check(default));
        if (expectedWrites == 0)
        {
            Assert.False(store.Contains("test"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => tweak.Restore(default));
            return;
        }

        await tweak.Restore(default);
        Assert.Equal(original, current);
        Assert.False(store.Contains("test"));
    }

    [Fact]
    public async Task FailedReadBackKeepsTheRestoreSnapshot()
    {
        using var workspace = new TemporaryWorkspace();
        var store = workspace.Backups;
        store.Save("test", "before", "String", true);
        var tweak = AdditionalTweaks.Stored("test", "Title", "Title", "Description", "Description", "desired",
            _ => Task.FromResult("desired"), (_, _) => Task.CompletedTask, store);

        await Assert.ThrowsAsync<IOException>(() => tweak.Restore(default));
        Assert.Equal("before", store.Get("test")?.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("from-user")]
    [InlineData("7")]
    public async Task RegistryTweakRestoresOriginalValueAndKind(string? original)
    {
        using var workspace = new TemporaryWorkspace();
        var store = workspace.Backups;
        string path = @"Software\MagnumTweaker.Tests." + Guid.NewGuid().ToString("N");
        using var hive = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
        using var key = hive.CreateSubKey(path, RegistryKeyPermissionCheck.ReadWriteSubTree, RegistryOptions.Volatile);
        if (original != null) key.SetValue("Value", original, RegistryValueKind.String);
        var tweak = new RegistryTweakFactory(store).Create("registry", "Title", "Description", "Category", "Impact",
            TweakRisk.Safe, false, RegistryHive.CurrentUser, path, "Value", 7, RegistryValueKind.DWord);
        try
        {
            Assert.Equal(TweakState.NotApplied, await tweak.Check(default));
            await tweak.Apply(default);
            Assert.Equal(TweakState.Applied, await tweak.Check(default));
            Assert.Equal(RegistryValueKind.DWord, key.GetValueKind("Value"));
            await tweak.Restore(default);
            Assert.Equal(original, key.GetValue("Value"));
            if (original != null) Assert.Equal(RegistryValueKind.String, key.GetValueKind("Value"));
            Assert.False(tweak.CanRestore);
        }
        finally
        {
            hive.DeleteSubKey(path, throwOnMissingSubKey: false);
        }
    }
}
