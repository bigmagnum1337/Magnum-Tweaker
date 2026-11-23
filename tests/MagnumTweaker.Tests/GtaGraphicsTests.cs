using System.Text;
using System.Xml.Linq;

namespace MagnumTweaker.Tests;

public sealed class GtaGraphicsTests
{
    [Theory]
    [InlineData("Test GPU &amp; Co", "Test GPU & Co")]
    [InlineData("AMD Test Adapter", "AMD Test Adapter")]
    public async Task PresetPreservesGpuAndRestoresExactOriginalBytes(string encodedGpu, string expectedGpu)
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("settings.xml");
        byte[] original = Encoding.UTF8.GetBytes($"<Settings>\r\n  <VideoCardDescription>{encodedGpu}</VideoCardDescription>\r\n</Settings>\r\n");
        File.WriteAllBytes(path, original);
        var tweak = GtaGraphics.Create(workspace.Backups, path, () => false);

        Assert.Equal(TweakState.NotApplied, await tweak.Check(default));
        await tweak.Apply(default);
        await tweak.Apply(default);
        var profile = XDocument.Load(path);
        Assert.Equal(expectedGpu, profile.Root?.Element("VideoCardDescription")?.Value);
        Assert.Equal("239", (string?)profile.Root?.Element("video")?.Element("RefreshRate")?.Attribute("value"));
        Assert.Equal(TweakState.Applied, await tweak.Check(default));

        await tweak.Restore(default);
        Assert.Equal(original, File.ReadAllBytes(path));
        Assert.Equal(TweakState.NotApplied, await tweak.Check(default));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("<Settings/>")]
    public void PresetRequiresAnExistingGpuDescription(string? contents)
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("settings.xml");
        if (contents != null) File.WriteAllText(path, contents);
        var tweak = GtaGraphics.Create(workspace.Backups, path, () => false);

        Assert.Throws<NotSupportedException>(() => { _ = tweak.Apply(default); });
        Assert.Equal(contents != null, File.Exists(path));
        if (contents != null) Assert.Equal(contents, File.ReadAllText(path));
        Assert.False(tweak.CanRestore);
    }

    [Fact]
    public void RunningGamePreventsReplacementAndKeepsOriginalFile()
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("settings.xml");
        const string original = "<Settings><VideoCardDescription>GPU</VideoCardDescription></Settings>";
        File.WriteAllText(path, original);
        var tweak = GtaGraphics.Create(workspace.Backups, path, () => true);

        Assert.Throws<IOException>(() => { _ = tweak.Apply(default); });
        Assert.Equal(original, File.ReadAllText(path));
    }

    [Fact]
    public void PresetRejectsXmlDtdsBeforeSavingOrReplacingAnything()
    {
        using var workspace = new TemporaryWorkspace();
        string path = workspace.FilePath("settings.xml");
        const string original = "<!DOCTYPE Settings [<!ENTITY gpu 'Injected'>]><Settings><VideoCardDescription>&gpu;</VideoCardDescription></Settings>";
        File.WriteAllText(path, original);
        var tweak = GtaGraphics.Create(workspace.Backups, path, () => false);

        Assert.Throws<System.Xml.XmlException>(() => { _ = tweak.Apply(default); });
        Assert.Equal(original, File.ReadAllText(path));
        Assert.False(File.Exists(workspace.FilePath("backup.json")));
    }
}
