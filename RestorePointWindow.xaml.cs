using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Automation.Peers;
using MagnumTweaker.Services;

namespace MagnumTweaker;

public partial class RestorePointWindow : Window
{
    private readonly Func<Task<RestorePointResult>> _createPoint;
    private bool _creating;
    private bool _closing;
    internal bool PointCreated { get; private set; }

    public RestorePointWindow() : this(RestorePoint.RequestAsync) { }

    internal RestorePointWindow(Func<Task<RestorePointResult>> createPoint)
    {
        _createPoint = createPoint;
        InitializeComponent();
        Title = Heading.Text = UiLanguage.Text("Точка восстановления", "Restore point");
        Explanation.Text = UiLanguage.Text(
            "Перед первым твиком рекомендуем создать точку восстановления. Она сохраняет состояние системных файлов, реестра и настроек Windows. Если возникнет проблема, «Восстановление системы» поможет вернуть их к этому состоянию. Личные файлы требуют отдельной резервной копии.",
            "We recommend creating a restore point before applying your first tweak. It saves the state of Windows system files, the registry and settings. If a problem occurs, System Restore can return them to that state. Back up your personal files separately.");
        Status.Text = UiLanguage.Text("Windows может запросить права администратора.",
            "Windows may ask for administrator permission.");
        CreateButton.Content = UiLanguage.Text("Создать точку", "Create restore point");
        ContinueButton.Content = UiLanguage.Text("Продолжить без точки", "Continue without a restore point");
        WindowTransitions.PrepareEntrance(this);
        Loaded += (_, _) => CreateButton.Focus();
    }

    private async void Create_Click(object sender, RoutedEventArgs e)
    {
        if (_creating || _closing || PointCreated) return;
        _creating = true;
        CreateButton.IsEnabled = ContinueButton.IsEnabled = CloseButton.IsEnabled = false;
        SetStatus(UiLanguage.Text("Windows создаёт точку восстановления…", "Windows is creating a restore point…"));
        RestorePointResult result;
        try
        {
            result = await _createPoint();
        }
        catch (Win32Exception)
        {
            result = RestorePointResult.NotConfirmed;
        }
        finally
        {
            _creating = false;
            CreateButton.IsEnabled = ContinueButton.IsEnabled = CloseButton.IsEnabled = true;
        }

        if (result == RestorePointResult.Created)
        {
            PointCreated = true;
            CreateButton.Visibility = Visibility.Collapsed;
            ContinueButton.Content = UiLanguage.Text("Продолжить", "Continue");
            Status.Foreground = new SolidColorBrush(Color.FromRgb(82, 219, 153));
            SetStatus(UiLanguage.Text("Точка восстановления создана.", "Restore point created."));
            if (SystemParameters.ClientAreaAnimation)
                Status.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(220)));
            ((App)Application.Current).PlaySound(InterfaceSound.Success);
            ContinueButton.Focus();
            return;
        }

        SetStatus(result switch
        {
            RestorePointResult.Cancelled => UiLanguage.Text("Запрос прав администратора отменён.", "The administrator prompt was cancelled."),
            RestorePointResult.RequiresAdmin => UiLanguage.Text("Для создания точки нужны права администратора.", "Creating a restore point requires administrator permission."),
            _ => UiLanguage.Text(
                "Windows не подтвердила создание точки. Проверьте, включена ли защита системы и не создавалась ли точка за последние 24 часа.",
                "Windows could not confirm creation. Check whether System Protection is enabled and whether a point was created in the last 24 hours.")
        });
        CreateButton.Focus();
    }

    private void SetStatus(string text)
    {
        Status.Text = text;
        UIElementAutomationPeer.FromElement(Status)?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private async void Continue_Click(object sender, RoutedEventArgs e) => await FinishAsync(true);
    private async void Close_Click(object sender, RoutedEventArgs e) => await FinishAsync(false);

    protected override async void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if (e.Key != Key.Escape) return;
        e.Handled = true;
        await FinishAsync(false);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (_creating) e.Cancel = true;
    }

    private async Task FinishAsync(bool proceed)
    {
        if (_creating || _closing) return;
        _closing = true;
        await WindowTransitions.FadeOutAsync(this);
        if (IsVisible) DialogResult = proceed;
    }
}
