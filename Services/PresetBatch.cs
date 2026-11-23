using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal enum PresetBatchResult { Applied = 0, Partial = 2, Busy = 3, Rejected = 4 }

internal sealed record PresetBatchRequest(string Language, string Profile, string UserSid, string[] TweakIds)
{
    internal const string Command = "--apply-preset";

    internal string[] Arguments() => [Command, Language, Profile, UserSid, .. TweakIds];

    internal bool CanRun(bool elevated, string? currentSid) =>
        elevated && string.Equals(UserSid, currentSid, StringComparison.Ordinal);

    internal static bool TryParse(string[] args, out PresetBatchRequest? request)
    {
        request = null;
        if (args is not [Command, "ru" or "en", "10" or "11", var sid, .. var identifiers] ||
            identifiers.Length is 0 or > 128 || string.IsNullOrWhiteSpace(sid) ||
            identifiers.Any(string.IsNullOrWhiteSpace) ||
            identifiers.Distinct(StringComparer.Ordinal).Count() != identifiers.Length)
            return false;

        request = new(args[1], args[2], sid, identifiers);
        return true;
    }

    internal Tweak[]? Resolve(IEnumerable<Tweak> profile)
    {
        var requested = TweakIds.ToHashSet(StringComparer.Ordinal);
        var members = GamingPreset.Select(profile).Where(t => requested.Contains(t.Id)).ToArray();
        // Resolve against the preset allowlist in catalog order before making any writes.
        return members.Length == TweakIds.Length ? members : null;
    }
}

internal static class PresetBatch
{
    internal const string SettingsMutexName = @"Local\MagnumTweaker.SettingsWrite";
    internal static PresetBatchResult Apply(IEnumerable<Tweak> tweaks, Action reloadBackups,
        Func<Tweak, CancellationToken, Task<TweakState>> inspect)
    {
        // The caller runs this on a worker thread. Keep the entire batch under the same lock as single writes.
        using var gate = new Mutex(false, SettingsMutexName);
        bool acquired;
        try { acquired = gate.WaitOne(0); }
        catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) return PresetBatchResult.Busy;

        try
        {
            reloadBackups();
            bool partial = false;
            foreach (var tweak in tweaks)
            {
                try
                {
                    var state = inspect(tweak, CancellationToken.None).GetAwaiter().GetResult();
                    if (state == TweakState.Applied) continue;
                    if (state != TweakState.NotApplied)
                    {
                        partial = true;
                        continue;
                    }

                    tweak.Apply(CancellationToken.None).GetAwaiter().GetResult();
                    partial |= inspect(tweak, CancellationToken.None).GetAwaiter().GetResult() != TweakState.Applied;
                }
                catch (Exception)
                {
                    partial = true;
                }
            }

            return partial ? PresetBatchResult.Partial : PresetBatchResult.Applied;
        }
        finally
        {
            gate.ReleaseMutex();
        }
    }
}
