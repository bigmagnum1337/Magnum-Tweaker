namespace MagnumTweaker.Tests;

public sealed class PresetBatchTests
{
    [Fact]
    public void WorkerResolvesOnlyRequestedPresetCardsInCatalogOrder()
    {
        var request = new PresetBatchRequest("ru", "11", "S-1-5-21-101", ["tcp-rss", "game-mode"]);
        Assert.True(PresetBatchRequest.TryParse(request.Arguments(), out var parsed));
        Assert.Equal(request.TweakIds, parsed!.TweakIds);
        Assert.True(parsed.CanRun(true, request.UserSid));
        Assert.False(parsed.CanRun(false, request.UserSid));
        Assert.False(parsed.CanRun(true, "S-1-5-21-102"));

        Tweak[] profile = [Card("game-mode", TweakRisk.Safe), Card("hags", TweakRisk.Experimental), Card("tcp-rss")];
        Assert.Equal(new[] { "game-mode", "tcp-rss" }, parsed.Resolve(profile)!.Select(t => t.Id));
        Assert.Null((request with { TweakIds = ["game-mode", "hags"] }).Resolve(profile));
        Assert.False(PresetBatchRequest.TryParse((request with { TweakIds = ["game-mode", "game-mode"] }).Arguments(), out _));
    }

    [Fact]
    public void BatchSkipsAppliedCardsAndVerifiesEachWrite()
    {
        int writes = 0, reads = 0, reloads = 0;
        var current = TweakState.NotApplied;
        var applied = Card("ready", check: () => TweakState.Applied,
            apply: () => throw new InvalidOperationException("Already applied"));
        var pending = Card("pending", check: () => { reads++; return current; },
            apply: () => { writes++; current = TweakState.Applied; });
        Assert.Equal(PresetBatchResult.Applied, PresetBatch.Apply([applied, pending], () => reloads++, Inspect));
        Assert.Equal(1, reloads);
        Assert.Equal(1, writes);
        Assert.Equal(2, reads);
    }

    [Fact]
    public void BatchContinuesAfterFailureAndReportsPartialCompletion()
    {
        var current = TweakState.NotApplied;
        var failing = Card("failed", apply: () => throw new IOException("Unavailable write"));
        var unsupported = Card("unsupported", check: () => TweakState.Unsupported,
            apply: () => throw new InvalidOperationException("Unsupported settings must not be written"));
        var pending = Card("pending", check: () => current, apply: () => current = TweakState.Applied);
        Assert.Equal(PresetBatchResult.Partial, PresetBatch.Apply([failing, unsupported, pending], () => { }, Inspect));
        Assert.Equal(TweakState.Applied, current);
    }

    private static Task<TweakState> Inspect(Tweak tweak, CancellationToken ct) => tweak.Check(ct);

    private static Tweak Card(string id, TweakRisk risk = TweakRisk.Moderate, Func<TweakState>? check = null, Action? apply = null) => new()
    {
        Id = id, Title = id, Description = "", Category = "", Impact = "", Risk = risk,
        Check = _ => Task.FromResult(check?.Invoke() ?? TweakState.NotApplied),
        Apply = _ => { apply?.Invoke(); return Task.CompletedTask; },
        Restore = _ => Task.CompletedTask
    };
}
