namespace MagnumTweaker.Services;

internal sealed record ElevationRequest(string Action, string TweakId, string UserSid)
{
    internal const string Preset = "preset";
    internal const string Apply = "apply";
    internal const string Restore = "restore";

    internal static bool TryParse(string[] arguments, out ElevationRequest? request)
    {
        request = null;
        if (arguments is not ["--resume-setup", "en" or "ru", "10" or "11",
            "--action", var action, var id, var sid]) return false;
        if (string.IsNullOrWhiteSpace(sid) ||
            !(action == Preset && id == "-" || action is Apply or Restore && !string.IsNullOrWhiteSpace(id) && id != "-"))
            return false;
        request = new(action, id, sid);
        return true;
    }

    internal bool CanResume(bool elevated, string? currentSid) =>
        elevated && string.Equals(UserSid, currentSid, StringComparison.Ordinal);

    internal string[] Arguments(string language, string windowsProfile) =>
        ["--resume-setup", language, windowsProfile, "--action", Action, TweakId, UserSid];
}
