namespace MagnumTweaker.Tests;

public sealed class TweakOrderingTests
{
    [Theory]
    [InlineData(false, "b,d,a,c,e,f,g,h,i")]
    [InlineData(true, "a,c,b,d,e,f,g,h,i")]
    public void ApplicationSortingGroupsStatesAndPreservesCatalogOrder(bool appliedFirst, string expected)
    {
        Tweak[] source =
        [
            Card("a", TweakState.Applied), Card("b", TweakState.NotApplied),
            Card("c", TweakState.Applied), Card("d", TweakState.NotApplied),
            Card("e", TweakState.Unsupported), Card("f", TweakState.Unknown),
            Card("g", TweakState.Applied, readOnly: true), Card("h", TweakState.Error),
            Card("i", TweakState.Checking)
        ];
        var mode = appliedFirst ? TweakSort.AppliedFirst : TweakSort.NotAppliedFirst;
        Assert.Equal(expected.Split(','), TweakOrdering.Sort(source, mode).Select(tweak => tweak.Id));
    }

    [Fact]
    public void ReorderingReadsCurrentStateAfterApplyAndRestore()
    {
        var first = Card("first", TweakState.NotApplied);
        var second = Card("second", TweakState.Applied);
        Tweak[] source = [first, second];
        Assert.Equal(second, TweakOrdering.Sort(source, TweakSort.AppliedFirst).First());
        first.State = TweakState.Applied;
        second.State = TweakState.NotApplied;
        Assert.Equal(first, TweakOrdering.Sort(source, TweakSort.AppliedFirst).First());
    }

    private static Tweak Card(string id, TweakState state, bool readOnly = false) => new()
    {
        Id = id, Title = id, Description = "", Category = "", Impact = "", State = state, ReadOnly = readOnly,
        Check = _ => Task.FromResult(state), Apply = _ => Task.CompletedTask, Restore = _ => Task.CompletedTask
    };
}
