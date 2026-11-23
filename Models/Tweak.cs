using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MagnumTweaker.Models;

public enum TweakState { Unknown, Checking, Applied, NotApplied, Unsupported, Error }
public enum TweakRisk { Safe, Moderate, Experimental }

public sealed class Tweak : INotifyPropertyChanged
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string Category { get; init; }
    public required string Impact { get; init; }
    public TweakRisk Risk { get; init; }
    public bool RequiresRestart { get; init; }
    public string? SourceUrl { get; init; }
    public string? EnglishTitle { get; set; }
    public string? EnglishDescription { get; set; }
    public string? WindowsProfile { get; set; }
    public string? Hardware { get; set; }
    public string Mechanism { get; set; } = "";
    public bool ReadOnly { get; init; }
    public bool RequiresAdmin { get; init; } = true;
    public Func<bool>? HasBackup { get; init; }
    public string PlatformText => string.Join(" · ", new[] { WindowsProfile is null ? "Windows 10/11" : $"Windows {WindowsProfile}", Hardware }.Where(s => !string.IsNullOrEmpty(s)));
    public string WindowsText => WindowsProfile is null ? "Windows 10/11" : $"Windows {WindowsProfile}";
    public System.Windows.Visibility HardwareBadgeVisibility => string.IsNullOrEmpty(Hardware) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
    public System.Windows.FontStyle HardwareBadgeFontStyle => ReadOnly ? System.Windows.FontStyles.Italic : System.Windows.FontStyles.Normal;
    public System.Windows.FontWeight HardwareBadgeWeight => ReadOnly ? System.Windows.FontWeights.Bold : System.Windows.FontWeights.Normal;
    public string HardwareIcon => ReadOnly ? "M0,8 L2,1 L5,5 L10,1 L8,8" : Category == "GTA V"
        ? "M0,0 H5 L8,8 L11,0 H16 L9,13 H7 Z" : "M1,1 H9 V7 H1 Z M3,9 H7 M5,7 V9";
    public string DisplayTitle => UiLanguage.IsEnglish ? EnglishTitle ?? (EnglishTweakText.Entries.TryGetValue(Id, out var t) ? t.Title : Title) : Title;
    public string DisplayDescription => UiLanguage.IsEnglish ? EnglishDescription ?? (EnglishTweakText.Entries.TryGetValue(Id, out var t) ? t.Description : Description) : Description;
    public bool CanApply => !ReadOnly && State == TweakState.NotApplied;
    public bool CanRestore => !ReadOnly && State is not (TweakState.Unknown or TweakState.Checking or TweakState.Unsupported)
        && HasBackup?.Invoke() == true;
    public required Func<CancellationToken, Task<TweakState>> Check { get; init; }
    public required Func<CancellationToken, Task> Apply { get; init; }
    public required Func<CancellationToken, Task> Restore { get; init; }

    private bool _detailsExpanded;
    public bool DetailsExpanded
    {
        get => _detailsExpanded;
        set { _detailsExpanded = value; OnPropertyChanged(); }
    }
    public bool ShowApply => CanApply && !CanRestore;

    private TweakState _state;
    private bool _isActivationHighlighted;

    public bool IsActivationHighlighted
    {
        get => _isActivationHighlighted;
        internal set
        {
            if (_isActivationHighlighted == value) return;
            _isActivationHighlighted = value;
            OnPropertyChanged();
        }
    }
    public TweakState State { get => _state; set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateText)); OnPropertyChanged(nameof(StateColor)); OnPropertyChanged(nameof(CanApply)); OnPropertyChanged(nameof(ShowApply)); OnPropertyChanged(nameof(CanRestore)); } }
    public string StateText => State switch
    {
        TweakState.Applied when ReadOnly => UiLanguage.Text("ЗАПУСК РАЗРЕШЁН", "STARTUP ALLOWED"),
        TweakState.NotApplied when ReadOnly => UiLanguage.Text("ПРОВЕРЬТЕ СЛУЖБУ", "CHECK SERVICE"),
        TweakState.Applied => UiLanguage.Text("ПРИМЕНЕНО", "APPLIED"),
        TweakState.NotApplied => UiLanguage.Text("НЕ ПРИМЕНЕНО", "NOT APPLIED"),
        TweakState.Checking => UiLanguage.Text("ПРОВЕРКА", "CHECKING"),
        TweakState.Unsupported => UiLanguage.Text("НЕ ПОДДЕРЖИВАЕТСЯ", "UNSUPPORTED"),
        TweakState.Error => UiLanguage.Text("ОШИБКА", "ERROR"),
        _ => UiLanguage.Text("НЕ ПРОВЕРЕНО", "NOT CHECKED")
    };
    public string StateColor => State switch
    {
        TweakState.Applied => "#35D07F", TweakState.NotApplied => "#FF5D70",
        TweakState.Checking => "#F7C948", TweakState.Unsupported => "#667085",
        TweakState.Error => "#FF8A4C", _ => "#667085"
    };
    public string RiskText => Risk switch
    {
        TweakRisk.Safe => UiLanguage.Text("Безопасный", "Safe"),
        TweakRisk.Moderate => UiLanguage.Text("Умеренный", "Moderate"),
        _ => UiLanguage.Text("Экспериментальный", "Experimental")
    };
    public string RestartText => RequiresRestart
        ? UiLanguage.Text("Перезагрузка", "Restart required")
        : UiLanguage.Text("Без перезагрузки", "No restart");

    public event PropertyChangedEventHandler? PropertyChanged;
    internal void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(DisplayTitle));
        OnPropertyChanged(nameof(DisplayDescription));
        OnPropertyChanged(nameof(StateText));
        OnPropertyChanged(nameof(RiskText));
        OnPropertyChanged(nameof(RestartText));
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
