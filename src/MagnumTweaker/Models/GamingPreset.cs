namespace MagnumTweaker.Models;

internal static class GamingPreset
{
    internal static IEnumerable<Tweak> Select(IEnumerable<Tweak> profile) =>
        profile.Where(tweak => !tweak.ReadOnly && (tweak.Risk == TweakRisk.Safe || tweak.IncludeInAcPreset ||
            tweak.Id is "menu-animation" or "game-config" or "taskbar-motion" or "tips" or "start-suggestions" or
                        "widgets11" or "search-highlights" or "aero-peek" or "controller-bar" or "tcp-rss" or "tcp-autotuning" or
                        "delivery-background-limit" or "combo-animation" or "tooltip-animation"));

    internal static (int Applied, int Available, int Unavailable) Count(IEnumerable<Tweak> members)
    {
        int applied = 0, available = 0, unavailable = 0;
        foreach (var tweak in members)
        {
            if (tweak.State == TweakState.Applied) applied++;
            else if (tweak.CanApply) available++;
            else unavailable++;
        }
        return (applied, available, unavailable);
    }
}
