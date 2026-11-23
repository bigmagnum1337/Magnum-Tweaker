using System.Text.Json;

namespace MagnumTweaker.Tests;

public sealed class PresetAndResourcesTests
{
    [Theory]
    [InlineData("10")]
    [InlineData("11")]
    public void PresetIncludesGtaAndSkipsExperimentalAndReadOnlyCards(string profile)
    {
        using var workspace = new TemporaryWorkspace();
        var members = GamingPreset.Select(new TweakCatalog(workspace.Backups).ForProfile(profile)).ToArray();
        Assert.Contains(members, tweak => tweak.Id == "gta-preset");
        Assert.Contains(members, tweak => tweak.Id == "game-mode");
        Assert.DoesNotContain(members, tweak => tweak.ReadOnly || tweak.Risk == TweakRisk.Experimental);
        Assert.DoesNotContain(members, tweak => tweak.Id is "ultimate" or "high-performance");
        Assert.Contains(members, tweak => tweak.Id == (profile == "10" ? "start-suggestions" : "widgets11"));
    }

    [Fact]
    public void PowerPresetIncludesExactlyOneValuePerAcSettingInDependencyOrder()
    {
        using var workspace = new TemporaryWorkspace();
        var cards = AcPowerSettings.Create(workspace.Backups).ToArray();
        var chosen = GamingPreset.Select(cards).ToArray();
        Assert.Equal(14, chosen.Length);
        Assert.All(chosen, card => Assert.Equal(TweakRisk.Moderate, card.Risk));
        Assert.Equal(chosen.Length, chosen.Select(card => card.Id[..card.Id.LastIndexOf('-')]).Distinct().Count());
        Assert.Contains(chosen, card => card.Hardware == "Wi-Fi");
        Assert.Contains(chosen, card => card.Hardware == "PCIe");
        Assert.Contains(chosen, card => card.Hardware == "USB");
        Assert.DoesNotContain(chosen, card => card.Id.EndsWith("-25") || card.Id.EndsWith("-50") || card.Id.EndsWith("-4"));
        int maximum = Array.FindIndex(chosen, card => card.Id.StartsWith("ac-ea062031"));
        int minimum = Array.FindIndex(chosen, card => card.Id.StartsWith("ac-0cc5b647"));
        Assert.InRange(maximum, 0, minimum - 1);
    }

    [Fact]
    public void UnsupportedAcSettingsRemainInThePresetCountWithoutAnApplyAction()
    {
        using var workspace = new TemporaryWorkspace();
        var member = GamingPreset.Select(AcPowerSettings.Create(workspace.Backups)).First();
        member.State = TweakState.Unsupported;

        Assert.Equal((0, 0, 1), GamingPreset.Count([member]));
        Assert.False(member.ShowApply);
    }

    [Fact]
    public void PresetCountsMatchTheLatestVerifiedStates()
    {
        using var workspace = new TemporaryWorkspace();
        var members = GamingPreset.Select(new TweakCatalog(workspace.Backups).ForProfile("11")).Take(4).ToArray();
        members[0].State = TweakState.Applied;
        members[1].State = TweakState.NotApplied;
        members[2].State = TweakState.Error;
        members[3].State = TweakState.Unsupported;

        Assert.Equal((1, 1, 2), GamingPreset.Count(members));
        members[1].State = TweakState.Applied;
        Assert.Equal((2, 0, 2), GamingPreset.Count(members));
        Assert.Equal((0, 0, 0), GamingPreset.Count([]));
    }

    [Fact]
    public void EmbeddedGuideContainsAllNumberedSourceEntries()
    {
        using var input = typeof(App).Assembly.GetManifestResourceStream("MagnumTweaker.Reference.json");
        Assert.NotNull(input);
        using var guide = JsonDocument.Parse(input);
        var entries = guide.RootElement.EnumerateArray().ToArray();
        var sourceIds = entries.Select(entry => entry.GetProperty("id").GetInt32()).Where(id => id > 0).Order().ToArray();

        Assert.Equal(236, entries.Length);
        Assert.Equal(Enumerable.Range(1, 217), sourceIds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(100)]
    public void HoverSoundUsesMonoPcmAndRespectsVolume(int volume)
    {
        using var input = HoverSound.CreateWave(volume, InterfaceSound.Hover);
        using var wave = new BinaryReader(input);

        Assert.Equal("RIFF", new string(wave.ReadChars(4)));
        Assert.Equal(input.Length - 8, wave.ReadUInt32());
        Assert.Equal("WAVEfmt ", new string(wave.ReadChars(8)));
        Assert.True(wave.ReadUInt32() >= 16);
        Assert.Equal(1, wave.ReadUInt16());
        Assert.Equal(1, wave.ReadUInt16());
        var sampleRate = wave.ReadUInt32();
        Assert.Equal(sampleRate * 2, wave.ReadUInt32());
        Assert.Equal(2, wave.ReadUInt16());
        Assert.Equal(16, wave.ReadUInt16());
        Assert.Equal("data", new string(wave.ReadChars(4)));
        int samples = wave.ReadInt32() / sizeof(short);
        int peak = Enumerable.Range(0, samples).Max(_ => Math.Abs((int)wave.ReadInt16()));
        if (volume == 0) Assert.Equal(0, peak);
        else Assert.InRange(peak, 1, volume * 26);
    }
}
