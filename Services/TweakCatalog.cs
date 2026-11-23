using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Win32;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

public sealed class TweakCatalog
{
    private readonly BackupStore _backups = new();
    internal void ReloadBackups() => _backups.Reload();
    public List<Tweak> Items { get; }
    public List<Tweak> ForProfile(string windows) => Items.Where(t => t.WindowsProfile is null || t.WindowsProfile == windows).ToList();

    public TweakCatalog()
    {
        var backups = _backups;
        var f = new RegistryTweakFactory(backups);
        const string msGame = "https://learn.microsoft.com/windows/apps/develop/settings/settings-windows-11";
        const string msDo = "https://learn.microsoft.com/windows/deployment/do/waas-delivery-optimization-reference";
        Items =
        [
            f.Create("game-mode", "Игровой режим", "Даёт игре приоритет на ресурсы, особенно когда в фоне есть нагрузка.", "FPS и игры", "Стабильность кадров", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Software\Microsoft\GameBar", "AutoGameModeEnabled", 1, RegistryValueKind.DWord, msGame),
            f.Create("game-dvr", "Отключить фоновую запись Game DVR", "Запрещает постоянный захват последних секунд игры в фоне.", "FPS и игры", "GPU / дисковая нагрузка", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\GameDVR", "AppCaptureEnabled", 0, RegistryValueKind.DWord, msGame),
            f.Create("game-dvr-policy", "Запретить Windows Game Recording", "Системная политика отключает Game DVR для всех обычных приложений.", "FPS и игры", "Фоновая запись", TweakRisk.Moderate, true, RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\GameDVR", "AllowGameDVR", 0, RegistryValueKind.DWord, msGame),
            f.Create("hags", "Аппаратное планирование GPU", "Выставляет запрос HAGS в Windows. Точка показывает сохранённый флаг. Для работы после перезапуска нужна поддержка GPU и драйвера.", "FPS и игры", "Задержка / FPS", TweakRisk.Experimental, true, RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers", "HwSchMode", 2, RegistryValueKind.DWord, "https://devblogs.microsoft.com/directx/hardware-accelerated-gpu-scheduling/"),
            f.Create("power-throttling", "Отключить Power Throttling", "Не позволяет Windows снижать частоту фоновых процессов ради экономии энергии.", "Производительность", "CPU", TweakRisk.Moderate, true, RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "PowerThrottlingOff", 1, RegistryValueKind.DWord, "https://learn.microsoft.com/windows-server/administration/performance-tuning/"),
            f.Create("presence-qos", "Не снижать QoS при бездействии", "Не позволяет Windows переводить приложение в более экономичный QoS, если долго нет ввода. Полезно для бенчмарков и игр с контроллером.", "Производительность", "CPU / планировщик", TweakRisk.Moderate, true, RegistryHive.LocalMachine, @"SYSTEM\CurrentControlSet\Control\Power\PowerThrottling", "DisableUserPresenceQos", 1, RegistryValueKind.DWord, "https://learn.microsoft.com/windows/win32/procthread/quality-of-service"),
            f.Create("mouse-accel", "Отключить ускорение мыши", "Выключает Enhance pointer precision для более предсказуемого движения указателя.", "Input lag", "Мышь", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseSpeed", "0", RegistryValueKind.String, "https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-systemparametersinfoa"),
            f.Create("mouse-threshold1", "Линейный порог мыши I", "Первая часть отключения системной акселерации мыши.", "Input lag", "Мышь", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold1", "0", RegistryValueKind.String),
            f.Create("mouse-threshold2", "Линейный порог мыши II", "Вторая часть отключения системной акселерации мыши.", "Input lag", "Мышь", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Control Panel\Mouse", "MouseThreshold2", "0", RegistryValueKind.String),
            f.Create("menu-delay", "Быстрое открытие меню", "Уменьшает искусственную задержку появления системных меню.", "Отзывчивость", "Интерфейс", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Control Panel\Desktop", "MenuShowDelay", "0", RegistryValueKind.String),
            f.Create("window-animations", "Отключить анимации окон", "Убирает анимацию сворачивания и разворачивания окон.", "Отзывчивость", "Интерфейс / GPU", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Control Panel\Desktop\WindowMetrics", "MinAnimate", "0", RegistryValueKind.String),
            f.Create("transparency", "Отключить прозрачность Windows", "Убирает системные эффекты прозрачности и небольшую нагрузку композиции.", "Отзывчивость", "Интерфейс / GPU", TweakRisk.Safe, false, RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "EnableTransparency", 0, RegistryValueKind.DWord),
            f.Create("visual-effects", "Визуальные эффекты: быстродействие", "Переключает Windows на профиль минимальных визуальных эффектов.", "Отзывчивость", "Интерфейс", TweakRisk.Moderate, false, RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", "VisualFXSetting", 2, RegistryValueKind.DWord),
            f.Create("delivery-p2p", "Отключить P2P обновлений", "Запрещает раздачу и получение обновлений с других ПК, сохраняя обычную загрузку Microsoft.", "Сеть", "Фоновый трафик", TweakRisk.Safe, true, RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DODownloadMode", 0, RegistryValueKind.DWord, msDo),
            f.Create("background-apps", "Ограничить приложения Microsoft Store", "Запрещает UWP-приложениям работать в фоне. Уведомления некоторых приложений могут пропасть.", "Производительность", "CPU / память", TweakRisk.Moderate, true, RegistryHive.LocalMachine, @"SOFTWARE\Policies\Microsoft\Windows\AppPrivacy", "LetAppsRunInBackground", 2, RegistryValueKind.DWord, "https://learn.microsoft.com/windows/client-management/mdm/policy-csp-privacy"),
            f.Create("search-web", "Отключить веб-поиск в Пуск", "Убирает интернет-запросы и веб-результаты при локальном поиске.", "Производительность", "Фоновая сеть", TweakRisk.Safe, true, RegistryHive.CurrentUser, @"Software\Policies\Microsoft\Windows\Explorer", "DisableSearchBoxSuggestions", 1, RegistryValueKind.DWord, "https://learn.microsoft.com/windows/privacy/manage-connections-from-windows-operating-system-components-to-microsoft-services"),
            f.Create("notifications", "Отключить toast-уведомления", "Не даёт всплывающим уведомлениям отвлекать и появляться поверх игры.", "FPS и игры", "Фокус / оверлеи", TweakRisk.Moderate, false, RegistryHive.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\PushNotifications", "ToastEnabled", 0, RegistryValueKind.DWord),
        ];
        var deliveryLimit = f.Create("delivery-background-limit", "Лимит фоновых загрузок обновлений",
            "Ограничивает фоновые загрузки через оптимизацию доставки до 20% доступной полосы. Фоновые обновления могут скачиваться дольше.",
            "Сеть", "Фоновый трафик", TweakRisk.Moderate, false, RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Windows\DeliveryOptimization", "DOPercentageMaxBackgroundBandwidth", 20, RegistryValueKind.DWord, msDo);
        deliveryLimit.EnglishTitle = "Limit background update downloads";
        deliveryLimit.EnglishDescription = "Limits Delivery Optimization background downloads to 20% of available bandwidth. Background downloads may take longer.";
        Items.Add(deliveryLimit);

        var widgets = f.Create("widgets-policy", "Отключить панель виджетов Windows 11",
            "Запрещает открытие панели виджетов системной политикой. Поддерживается в Pro, Enterprise и Education.",
            "Производительность", "Фоновая активность", TweakRisk.Moderate, true, RegistryHive.LocalMachine,
            @"SOFTWARE\Policies\Microsoft\Dsh", "AllowNewsAndInterests", 0, RegistryValueKind.DWord,
            "https://learn.microsoft.com/windows/client-management/mdm/policy-csp-newsandinterests");
        widgets.WindowsProfile = "11";
        widgets.EnglishTitle = "Disable Windows 11 Widgets";
        widgets.EnglishDescription = "Turns off the Widgets board through policy on supported Pro, Enterprise and Education editions.";
        Items.Add(widgets);

        Items.AddRange(AdditionalTweaks.Build(f, backups));
        Items.Add(GtaGraphics.Create(backups,System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Rockstar Games","GTA V","settings.xml")));
        Items.Single(x => x.Id == "hags").Hardware = "GPU · WDDM 2.7+";
    }

    public static bool IsAdministrator() => new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

    public void SetGame(string? path)
    {
        Items.RemoveAll(t => t.Id.StartsWith("game-exe-"));
        if (!string.IsNullOrWhiteSpace(path)) Items.AddRange(GameOptions.Create(path, _backups));
    }
}
