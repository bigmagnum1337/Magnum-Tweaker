using System.ComponentModel;
using System.Runtime.InteropServices;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal static class MenuAnimationTweak
{
    private const uint GetMenuAnimation = 0x1002;
    private const uint SetMenuAnimation = 0x1003;
    private const uint PersistAndNotify = 0x03;

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadSetting(uint action, uint param, out int value, uint flags);

    [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool WriteSetting(uint action, uint param, IntPtr value, uint flags);

    internal static Tweak Create(BackupStore backups)
    {
        var tweak = AdditionalTweaks.Stored("menu-animation", "Открывать меню Windows без анимации", "Open Windows menus without animation",
            "Убирает анимацию появления системных меню, чтобы они отображались сразу. Это меняет скорость интерфейса, а не FPS в играх.",
            "Removes the transition when Windows displays a system menu. This changes UI responsiveness rather than game FPS.",
            "off", ct => Read(GetMenuAnimation, ct), (value, ct) => Write(SetMenuAnimation, value, ct),
            backups, "Отзывчивость", requiresAdmin: false);
        tweak.Mechanism = "SystemParametersInfoW · SPI_SETMENUANIMATION";
        return tweak;
    }

    internal static IEnumerable<Tweak> CreateAdditional(BackupStore backups)
    {
        yield return Build("combo-animation", "Открывать выпадающие списки без анимации", "Open drop-down lists without animation",
            "Убирает анимацию открытия стандартных списков Windows.", "Removes the opening animation for standard Windows combo boxes.",
            0x1004, 0x1005, "SPI_SETCOMBOBOXANIMATION");
        yield return Build("tooltip-animation", "Показывать подсказки Windows без анимации", "Show Windows tooltips without animation",
            "Убирает появление подсказок с переходом. Влияет на отзывчивость интерфейса Windows.", "Removes the reveal transition for Windows tooltips.",
            0x1016, 0x1017, "SPI_SETTOOLTIPANIMATION");

        Tweak Build(string id, string title, string englishTitle, string description, string englishDescription,
            uint read, uint write, string mechanism)
        {
            var tweak = AdditionalTweaks.Stored(id, title, englishTitle, description, englishDescription,
                "off", ct => Read(read, ct), (value, ct) => Write(write, value, ct), backups, "Отзывчивость", requiresAdmin: false);
            tweak.Mechanism = "SystemParametersInfoW · " + mechanism;
            return tweak;
        }
    }

    private static Task<string> Read(uint action, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (!ReadSetting(action, 0, out int enabled, 0)) throw new Win32Exception();
        return Task.FromResult(enabled == 0 ? "off" : "on");
    }

    private static Task Write(uint action, string value, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var enabled = value switch { "on" => new IntPtr(1), "off" => IntPtr.Zero, _ => throw new ArgumentException(nameof(value)) };
        if (!WriteSetting(action, 0, enabled, PersistAndNotify)) throw new Win32Exception();
        return Task.CompletedTask;
    }
}
