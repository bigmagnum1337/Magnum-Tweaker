using System.IO;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal sealed class PowerProfile
{
    private const string Template = "e9a42b02-d5df-448d-aa00-03f14749eb61";
    private readonly string _path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagnumTweaker", "ultimate.json");
    private sealed record Snapshot(Guid Previous, Guid Owned);
    private Snapshot? Read() => File.Exists(_path) ? JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(_path)) ?? throw new InvalidDataException() : null;
    private static Task<string> Power(CancellationToken ct, params string[] args) => SystemTool.Run("powercfg.exe", ct, args);
    [DllImport("powrprof.dll")]
    private static extern uint PowerGetActiveScheme(IntPtr root, out IntPtr pointer);
    [DllImport("kernel32.dll")]
    private static extern IntPtr LocalFree(IntPtr pointer);

    internal static Task<Guid> Active(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(ActiveGuid());
    }

    internal static Guid ActiveGuid()
    {
        var result = PowerGetActiveScheme(IntPtr.Zero, out var pointer);
        if (result != 0) throw new Win32Exception((int)result);
        try { return Marshal.PtrToStructure<Guid>(pointer); }
        finally { LocalFree(pointer); }
    }

    public Tweak Ultimate() => new()
    {
        Id = "ultimate", Category = "Производительность", Title = "Максимальная производительность",
        EnglishTitle = "Ultimate Performance", Impact = "CPU", Risk = TweakRisk.Moderate,
        Description = "Создаёт и активирует копию Ultimate Performance. Более агрессивное питание может повысить нагрев. Откат возвращает прежнюю схему.",
        EnglishDescription = "Creates and activates an Ultimate Performance plan. May increase heat. Restore switches back to your previous plan.",
        Mechanism = $"powercfg /duplicatescheme {Template} → /setactive",
        HasBackup = () => File.Exists(_path),
        Check = async ct =>
        {
            var current = await Active(ct);
            return current == Read()?.Owned || current == Guid.Parse(Template) ? TweakState.Applied : TweakState.NotApplied;
        },
        Apply = async ct =>
        {
            var current = await Active(ct);
            var state = Read();
            if (current == state?.Owned || current == Guid.Parse(Template)) return;
            state = new(current, state?.Owned ?? Guid.NewGuid());
            var tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            File.WriteAllText(tmp, JsonSerializer.Serialize(state));
            File.Move(tmp, _path, true);
            var id = state.Owned.ToString();
            if (!(await Power(ct, "/list")).Contains(id, StringComparison.OrdinalIgnoreCase))
                await Power(ct, "/duplicatescheme", Template, id);
            await Power(ct, "/setactive", id);
        },
        Restore = async ct =>
        {
            var state = Read() ?? throw new IOException(UiLanguage.Text("Нет сохранённой схемы.", "No saved plan."));
            if (await Active(ct) == state.Owned) await Power(ct, "/setactive", state.Previous.ToString());
            if ((await Power(ct, "/list")).Contains(state.Owned.ToString(), StringComparison.OrdinalIgnoreCase))
                await Power(ct, "/delete", state.Owned.ToString());
            File.Delete(_path);
        }
    };
}
