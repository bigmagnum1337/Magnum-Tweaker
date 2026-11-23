using System.Windows;

namespace MagnumTweaker;

internal static class UiLanguage
{
    public static bool IsEnglish { get; private set; }

    public static string Text(string russian, string english) => IsEnglish ? english : russian;

    public static void Select(bool english)
    {
        IsEnglish = english;
        var labels = new (string Key, string Russian, string English)[]
        {
            ("Categories", "КАТЕГОРИИ", "CATEGORIES"),
            ("Settings", "Настройки", "Settings"),
            ("Reference", "Справочник оптимизаций", "Optimization guide"),
            ("OpenSetting", "Открыть параметры Windows", "Open Windows settings"),
            ("ShowTweak", "Показать в каталоге", "Show in catalog"),
            ("ProfileLabel", "Профиль Windows", "Windows profile"),
            ("Details", "Описание твика", "Tweak details"),
            ("ToggleSidebar", "Свернуть или развернуть категории", "Collapse or expand categories"),
            ("Terms", "Пользовательское соглашение", "Terms of Use"),
            ("Language", "Язык", "Language"),
            ("LanguageDescription", "Язык меню, карточек и сообщений приложения", "Language for menus, tweak cards, and app messages"),
            ("HoverVolume", "Звук наведения", "Hover sound"),
            ("HoverVolumeDescription", "Передвиньте ползунок на 0%, чтобы выключить звук.", "Set the slider to 0% to mute hover sounds."),
            ("Minimize", "Свернуть", "Minimize"),
            ("Maximize", "Развернуть / восстановить", "Maximize / restore"),
            ("Close", "Закрыть", "Close"),
            ("ScanPrompt", "Запустите проверку, чтобы узнать состояние твиков.", "Run a scan to check the status of your tweaks."),
            ("Search", "Поиск твиков", "Search tweaks"),
            ("Sort", "Сортировка", "Sort"),
            ("Filters", "Фильтры", "Filters"),
            ("StatusFilter", "Состояние", "Status"),
            ("AnyStatus", "Любое состояние", "Any status"),
            ("AppliedFilter", "Применённые", "Applied"),
            ("NotAppliedFilter", "Не применённые", "Not applied"),
            ("UncheckedFilter", "Не проверенные", "Not checked"),
            ("RiskFilter", "Уровень риска", "Risk level"),
            ("AnyRisk", "Любой риск", "Any risk"),
            ("SafeFilter", "Безопасные", "Safe"),
            ("ModerateFilter", "Умеренные", "Moderate"),
            ("ExperimentalFilter", "Экспериментальные", "Experimental"),
            ("ResetFilters", "Сбросить фильтры", "Reset filters"),
            ("ScanAll", "Проверить всё", "Check all"),
            ("SafeSet", "Безопасный набор", "Safe preset"),
            ("ApplySafe", "Применить безопасные твики", "Apply safe tweaks"),
            ("Check", "Проверить", "Check"),
            ("Apply", "Применить", "Apply"),
            ("Restore", "Откат", "Restore"),
            ("Empty", "Твики не найдены", "No tweaks found"),
            ("Ready", "Готово", "Ready")
        };
        foreach (var label in labels)
            Application.Current.Resources[label.Key] = Text(label.Russian, label.English);
    }

    public static string Category(string category) => category switch
    {
        "Все твики" => Text(category, "All tweaks"),
        "FPS и игры" => Text(category, "FPS and gaming"),
        "Производительность" => Text(category, "Performance"),
        "Сеть" => Text(category, "Network"),
        "Отзывчивость" => Text(category, "Responsiveness"),
        _ => category
    };
}
