namespace MagnumTweaker.Tests;

public sealed class ElevationRequestTests
{
    [Theory]
    [InlineData("apply", "ac-test")]
    [InlineData("restore", "nic-setting with spaces")]
    [InlineData("preset", "-")]
    public void ElevatedActionRoundTripsAsSeparateArguments(string action, string tweakId)
    {
        var expected = new ElevationRequest(action, tweakId, "S-1-5-21-101");
        Assert.True(ElevationRequest.TryParse(expected.Arguments("ru", "11"), out var actual));
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData("execute", "unknown", "S-1-5-21-101")]
    [InlineData("preset", "named-tweak", "S-1-5-21-101")]
    [InlineData("apply", "-", "S-1-5-21-101")]
    [InlineData("restore", "named-tweak", "")]
    public void InvalidActionsDoNotResume(string action, string id, string sid)
    {
        Assert.False(ElevationRequest.TryParse(["--resume-setup", "en", "10", "--action", action, id, sid], out var request));
        Assert.Null(request);
    }

    [Theory]
    [InlineData(true, "S-1-5-21-101", true)]
    [InlineData(true, "S-1-5-21-102", false)]
    [InlineData(false, "S-1-5-21-101", false)]
    public void ResumeRequiresElevationUnderTheSameUser(bool elevated, string currentSid, bool expected)
    {
        var request = new ElevationRequest("apply", "test", "S-1-5-21-101");
        Assert.Equal(expected, request.CanResume(elevated, currentSid));
    }

    [Fact]
    public async Task MenuAnimationIsCheckedReadOnlyAndIncludedInTheGamingPreset()
    {
        using var workspace = new TemporaryWorkspace();
        var tweak = MenuAnimationTweak.Create(workspace.Backups);
        Assert.False(tweak.RequiresAdmin);
        Assert.Contains(tweak, GamingPreset.Select([tweak]));
        Assert.Contains(await tweak.Check(default), new[] { TweakState.Applied, TweakState.NotApplied });
        Assert.Empty(Directory.GetFiles(workspace.Root));
    }
}
