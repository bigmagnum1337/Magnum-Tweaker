namespace MagnumTweaker.Models;

internal enum TweakSort { Catalog, NameAscending, NameDescending, NotAppliedFirst, AppliedFirst }

internal static class TweakOrdering
{
    internal static IEnumerable<Tweak> Sort(IEnumerable<Tweak> tweaks, TweakSort mode) => mode switch
    {
        TweakSort.NameAscending => tweaks.OrderBy(tweak => tweak.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
        TweakSort.NameDescending => tweaks.OrderByDescending(tweak => tweak.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
        TweakSort.NotAppliedFirst => ByState(tweaks, TweakState.NotApplied),
        TweakSort.AppliedFirst => ByState(tweaks, TweakState.Applied),
        _ => tweaks
    };

    private static IEnumerable<Tweak> ByState(IEnumerable<Tweak> tweaks, TweakState first) =>
        // OrderBy keeps catalog order within each group, including diagnostics and unknown states.
        tweaks.OrderBy(tweak => tweak.ReadOnly ? 2 : tweak.State switch
        {
            TweakState.Applied or TweakState.NotApplied => tweak.State == first ? 0 : 1,
            _ => 2
        });
}
