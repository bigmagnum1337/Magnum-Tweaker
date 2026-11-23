using System.ComponentModel;
using System.Diagnostics;

namespace MagnumTweaker.Services;

internal enum RestorePointResult { Created = 0, NotConfirmed = 2, Cancelled = 3, RequiresAdmin = 4 }

internal static class RestorePoint
{
    internal const string WorkerCommand = "--create-restore-point";
    private const string ConfirmedMarker = "MAGNUM_RESTORE_POINT_CREATED";

    internal static async Task<RestorePointResult> RequestAsync()
    {
        if (TweakCatalog.IsAdministrator()) return await CreateAsync();

        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" };
        start.ArgumentList.Add(WorkerCommand);
        try
        {
            using var worker = Process.Start(start);
            if (worker == null) return RestorePointResult.NotConfirmed;
            await worker.WaitForExitAsync();
            return Enum.IsDefined(typeof(RestorePointResult), worker.ExitCode)
                ? (RestorePointResult)worker.ExitCode : RestorePointResult.NotConfirmed;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return RestorePointResult.Cancelled;
        }
    }

    internal static async Task<RestorePointResult> RunWorkerAsync(string[] args)
    {
        if (args is not [WorkerCommand] || !TweakCatalog.IsAdministrator())
            return RestorePointResult.RequiresAdmin;
        return await CreateAsync();
    }

    private static Task<RestorePointResult> CreateAsync() => CreateAsync(command =>
        SystemTool.Run(@"WindowsPowerShell\v1.0\powershell.exe", TimeSpan.FromMinutes(3),
            CancellationToken.None, "-NoProfile", "-NonInteractive", "-Command", command));

    internal static async Task<RestorePointResult> CreateAsync(Func<string, Task<string>> runPowerShell)
    {
        // Checkpoint-Computer has no output and Windows may skip creation. Match this request's unique name.
        string description = "Magnum Tweaker " + Guid.NewGuid().ToString("N");
        string command = "$ErrorActionPreference='Stop';$WarningPreference='SilentlyContinue';" +
            $"$description='{description}';" +
            "Checkpoint-Computer -Description $description -RestorePointType MODIFY_SETTINGS;" +
            "for ($i=0; $i -lt 5; $i++) {" +
            "if (Get-ComputerRestorePoint | Where-Object { $_.Description -eq $description }) {" +
            $"Write-Output '{ConfirmedMarker}'; break }};" +
            "Start-Sleep -Seconds 1 }";
        try
        {
            string output = await runPowerShell(command);
            return string.Equals(output, ConfirmedMarker, StringComparison.Ordinal)
                ? RestorePointResult.Created : RestorePointResult.NotConfirmed;
        }
        catch (Exception ex) when (ex is System.IO.IOException or Win32Exception or OperationCanceledException)
        {
            return RestorePointResult.NotConfirmed;
        }
    }
}
