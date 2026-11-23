using System.Diagnostics;
using System.IO;

namespace MagnumTweaker.Services;

internal static class SystemTool
{
    public static Task<string> Run(string relativeExe, CancellationToken token, params string[] arguments) =>
        Run(relativeExe, TimeSpan.FromSeconds(15), token, arguments);

    internal static async Task<string> Run(string relativeExe, TimeSpan limit, CancellationToken token, params string[] arguments)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(limit);
        var start = new ProcessStartInfo(Path.Combine(Environment.SystemDirectory, relativeExe))
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
        };
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new IOException(relativeExe);
        using var cancel = timeout.Token.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(true); } catch (InvalidOperationException) { }
        });
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(output, errors, process.WaitForExitAsync(timeout.Token));
        if (process.ExitCode != 0) throw new IOException($"{relativeExe} ({process.ExitCode}): {errors.Result.Trim()}");
        return output.Result.Trim();
    }
}
