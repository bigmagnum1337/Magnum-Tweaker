using Xunit.Abstractions;

namespace MagnumTweaker.Tests;

public sealed class CatalogAndScanTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData("10", "uwp10", "widgets-policy")]
    [InlineData("11", "widgets-policy", "uwp10")]
    public void ProfilesKeepOnlyMatchingOsCardsAndUniqueIds(string profile, string included, string excluded)
    {
        using var workspace = new TemporaryWorkspace();
        var catalog = new TweakCatalog(workspace.Backups);
        var items = catalog.ForProfile(profile);
        Assert.Contains(items, t => t.Id == included);
        Assert.DoesNotContain(items, t => t.Id == excluded);
        Assert.Equal(items.Count, items.Select(t => t.Id).Distinct().Count());
        Assert.All(items, t => Assert.True(t.WindowsProfile == null || t.WindowsProfile == profile));
        Assert.Single(items, t => t.Id == "gta-preset");
        output.WriteLine($"Windows {profile}: {items.Count(t => !t.Id.StartsWith("nic-", StringComparison.Ordinal))} base cards, {items.Count} on this host.");
    }

    [Fact]
    public void RpServiceCardsNeverOfferChanges()
    {
        using var workspace = new TemporaryWorkspace();
        var catalog = new TweakCatalog(workspace.Backups);
        var cards = catalog.Items.Where(t => t.Id.StartsWith("rp-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(4, cards.Length);
        Assert.All(cards, card =>
        {
            card.State = TweakState.NotApplied;
            Assert.False(card.ShowApply);
            Assert.False(card.CanRestore);
        });
    }

    [Theory]
    [InlineData(TweakState.NotApplied, false, true, false)]
    [InlineData(TweakState.Applied, true, false, true)]
    [InlineData(TweakState.Applied, false, false, false)]
    [InlineData(TweakState.Unsupported, true, false, false)]
    [InlineData(TweakState.Error, true, false, true)]
    public void CardOffersOnlyAnActionThatCanRun(TweakState state, bool backup, bool apply, bool restore)
    {
        Tweak card = Probe(_ => Task.FromResult(state), () => backup);
        card.State = state;
        Assert.Equal(apply, card.ShowApply);
        Assert.Equal(restore, card.CanRestore);
    }

    [Theory]
    [InlineData(false, TweakState.Error)]
    [InlineData(true, TweakState.Unsupported)]
    public async Task ScanDistinguishesUnsupportedFromFailedReads(bool unsupported, TweakState expected)
    {
        var card = Probe(_ => Task.FromException<TweakState>(unsupported ? new NotSupportedException() : new IOException()));
        Assert.Equal(expected, await ScanWindow.Inspect(card, default));
    }

    [Fact]
    public async Task CancelledScanDoesNotReadTheSetting()
    {
        int reads = 0;
        var card = Probe(_ => { reads++; return Task.FromResult(TweakState.Applied); });
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ScanWindow.Inspect(card, new CancellationToken(true)));
        Assert.Equal(0, reads);
    }

    private static Tweak Probe(Func<CancellationToken, Task<TweakState>> check, Func<bool>? hasBackup = null) => new()
    {
        Id = "probe", Title = "Title", Description = "Description", Category = "Category", Impact = "Impact",
        Check = check, Apply = _ => Task.CompletedTask, Restore = _ => Task.CompletedTask, HasBackup = hasBackup
    };
}
