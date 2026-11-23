using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using MagnumTweaker.Services;

namespace MagnumTweaker;

public partial class App : Application
{
    internal InterfacePreferences Preferences { get; } = InterfacePreferences.Load();
    private readonly HoverSound _hoverSound = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.FirstOrDefault() == PresetBatchRequest.Command)
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var result = await RunPresetWorkerAsync(e.Args);
            Environment.ExitCode = (int)result;
            Shutdown((int)result);
            return;
        }
        EventManager.RegisterClassHandler(typeof(ButtonBase), ButtonBase.ClickEvent, new RoutedEventHandler((_, _) => PlaySound(InterfaceSound.Click)));
        var hover = new MouseEventHandler(ReferenceControl_MouseEnter);
        EventManager.RegisterClassHandler(typeof(ButtonBase), UIElement.MouseEnterEvent, hover);
        EventManager.RegisterClassHandler(typeof(ComboBoxItem), UIElement.MouseEnterEvent, hover);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var splash = new StartupWindow();
        WindowTransitions.PrepareEntrance(splash);
        splash.Show();
        ElevationRequest.TryParse(e.Args, out var action);
        if (action is null) await Task.Delay(SystemParameters.ClientAreaAnimation ? 925 : 550);

        if (action != null || e.Args is ["--resume-setup", "en" or "ru", "10" or "11"])
        {
            OpenMain(e.Args[1] == "en", e.Args[2], splash, action);
            return;
        }

        var setup = new SetupWindow(Preferences.English, Preferences.WindowsProfile);
        DismissSplashWhenReady(setup, splash);
        if (setup.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        OpenMain(setup.English, setup.WindowsVersion);
    }

    private async Task<PresetBatchResult> RunPresetWorkerAsync(string[] args)
    {
        if (!PresetBatchRequest.TryParse(args, out var request) || request == null ||
            Preferences.AcceptedTermsVersion != TermsWindow.Version)
            return PresetBatchResult.Rejected;

        using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
        if (!request.CanRun(TweakCatalog.IsAdministrator(), identity.User?.Value))
            return PresetBatchResult.Rejected;

        try
        {
            UiLanguage.Select(request.Language == "en");
            var catalog = new TweakCatalog();
            var pending = request.Resolve(catalog.ForProfile(request.Profile));
            if (pending == null) return PresetBatchResult.Rejected;
            return await Task.Run(() => PresetBatch.Apply(pending, catalog.ReloadBackups, ScanWindow.Inspect));
        }
        catch (Exception)
        {
            return PresetBatchResult.Partial;
        }
    }

    private void OpenMain(bool english, string windowsVersion, Window? splash = null, ElevationRequest? action = null)
    {
        UiLanguage.Select(english);
        if (Preferences.AcceptedTermsVersion != TermsWindow.Version)
        {
            var terms = new TermsWindow(requireAcceptance: true);
            if (splash is not null)
            {
                DismissSplashWhenReady(terms, splash);
                splash = null;
            }
            if (terms.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            Preferences.AcceptedTermsVersion = TermsWindow.Version;
            Preferences.English = english;
            Preferences.Save();
        }

        Preferences.English = english;
        var catalog = new TweakCatalog();
        catalog.SetGame(Preferences.GamePath);
        var scan = new ScanWindow(catalog.ForProfile(windowsVersion));
        if (splash is not null)
            DismissSplashWhenReady(scan, splash);
        if (scan.ShowDialog() != true) { Shutdown(); return; }
        Preferences.WindowsProfile = windowsVersion;
        var window = new MainWindow(windowsVersion, catalog);
        MainWindow = window;
        if (action != null) window.ContentRendered += ResumeAfterRender;
        window.Show();

        async void ResumeAfterRender(object? sender, EventArgs e)
        {
            window.ContentRendered -= ResumeAfterRender;
            await window.ResumeActionAsync(action!);
        }
        ShutdownMode = ShutdownMode.OnMainWindowClose;
    }

    private static void DismissSplashWhenReady(Window next, Window splash)
    {
        next.Loaded += Dismiss;

        async void Dismiss(object sender, RoutedEventArgs e)
        {
            next.Loaded -= Dismiss;
            await WindowTransitions.FadeOutAsync(splash);
            if (splash.IsVisible) splash.Close();
        }
    }

    private void ReferenceControl_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is not FrameworkElement { IsEnabled: true, IsMouseOver: true } control) return;

        // Drop-down items sit in a Popup, so resolve their window via their ComboBox.
        var owner = control is ComboBoxItem item ? ItemsControl.ItemsControlFromItemContainer(item) : control;
        if (owner != null && Window.GetWindow(owner) is ReferenceWindow)
            PlaySound(InterfaceSound.Hover);
    }

    internal void PlaySound(InterfaceSound sound) => _hoverSound.Play(Preferences.HoverVolume, sound);

    protected override void OnExit(ExitEventArgs e)
    {
        if (MainWindow is MainWindow)
            Preferences.Save();
        _hoverSound.Dispose();
        base.OnExit(e);
    }
}
