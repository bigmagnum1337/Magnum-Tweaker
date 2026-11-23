using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using MagnumTweaker.Models;
using MagnumTweaker.Services;

namespace MagnumTweaker;

public partial class MainWindow : Window
{
    public static readonly DependencyProperty SidebarLabelsVisibilityProperty = DependencyProperty.Register(
        nameof(SidebarLabelsVisibility), typeof(Visibility), typeof(MainWindow), new PropertyMetadata(Visibility.Visible));
    public Visibility SidebarLabelsVisibility => (Visibility)GetValue(SidebarLabelsVisibilityProperty);

    private void SidebarToggle_Click(object sender, RoutedEventArgs e)
    {
        bool compact = SidebarLabelsVisibility == Visibility.Visible;
        var visibility = compact ? Visibility.Collapsed : Visibility.Visible;
        SetValue(SidebarLabelsVisibilityProperty, visibility);
        SidebarToggle.HorizontalAlignment = compact ? HorizontalAlignment.Center : HorizontalAlignment.Right;
        AnimateSidebarValue(Sidebar, WidthProperty, compact ? 64 : 215);
        AnimateSidebarValue(SidebarBrand, WidthProperty, compact ? 50 : 163);
        AnimateSidebarValue(SidebarArrowRotation, RotateTransform.AngleProperty, compact ? 180 : 0);
        SidebarHeading.Visibility = SidebarSettingsLabel.Visibility = ProfileText.Visibility = visibility;
        SidebarGear.Margin = compact ? new Thickness(0) : new Thickness(0, 0, 9, 0);
        if (compact) SidebarGuide.Content = "≡";
        else SidebarGuide.SetResourceReference(ContentControl.ContentProperty, "Reference");
    }

    private static void AnimateSidebarValue(IAnimatable target, DependencyProperty property, double value)
    {
        var element = (DependencyObject)target;
        double current = (double)element.GetValue(property);
        var animation = SystemParameters.ClientAreaAnimation ? EaseTo(current, value, 230) : null;
        element.SetValue(property, value);
        target.BeginAnimation(property, animation);
    }
    private SmoothScrollViewer? TweakScroll => TweaksList.Template?.FindName("PART_TweakScroll", TweaksList) as SmoothScrollViewer;
    private IEnumerable<Tweak> PresetItems => GamingPreset.Select(_profileItems);
    private void UpdatePreset()
    {
        var (applied, available, unavailable) = GamingPreset.Count(PresetItems);
        PresetCounts.Text = UiLanguage.Text(
            $"Применено {applied} · доступно {available} · недоступно {unavailable}",
            $"{applied} applied · {available} available · {unavailable} unavailable");
        PresetApply.IsEnabled = available != 0 && !_busy;
    }
    private readonly TweakCatalog _catalog;
    private List<Tweak> _profileItems;
    private Tweak[] _visible = [];
    private string _windowsVersion;
    private const string AllTweaks = "Все твики";
    private sealed record CategoryOption(string Key, string Icon)
    {
        public string Label => UiLanguage.Category(Key);
    }
    private bool _busy;
    private bool _viewReady;
    private bool _settingsClosing;
    private bool _minimizing;
    private bool _hasScanSummary;
    private int _categoryTransition;
    private bool _categoryChanging;
    private TweakState? _statusFilter;
    private TweakRisk? _riskFilter;
    public MainWindow(string windowsVersion, TweakCatalog catalog)
    {
        _catalog = catalog;
        _profileItems = catalog.ForProfile(windowsVersion);
        _windowsVersion = windowsVersion;
        InitializeComponent();
        WindowTransitions.PrepareEntrance(this);
        ProfileText.Text = $"Windows {_windowsVersion} · x64";
        ProfilePicker.ItemsSource = new[] { "Windows 10", "Windows 11" };
        ProfilePicker.SelectedIndex = windowsVersion == "11" ? 1 : 0;
        TweaksList.ItemsSource = _visible;
        CategoryList.ItemsSource = new CategoryOption[]
        {
            new(AllTweaks, "M1,1 H6 V6 H1 Z M9,1 H14 V6 H9 Z M1,9 H6 V14 H1 Z M9,9 H14 V14 H9 Z"),
            new("FPS и игры", "M5,4 H10 C13,4 14,7 14,10 C14,12 12,12 10,10 H5 C3,12 1,12 1,10 C1,7 2,4 5,4 Z M4,7 H8 M6,5 V9 M11,7 H11.5"),
            new("Input lag", "M3,6 A4.5,4.5 0 0 1 12,6 V10 A4.5,4.5 0 0 1 3,10 Z M7.5,2 V7"),
            new("Производительность", "M1,12 A6.5,6.5 0 0 1 14,12 M7.5,11 L11,6 M3,9 L4,9 M7.5,5 V6"),
            new("Сеть", "M1,5 Q7.5,0 14,5 M3,8 Q7.5,4 12,8 M5,11 Q7.5,8.5 10,11 M7,13 H8"),
            new("Отзывчивость", "M8,1 L2,8 H7 L6,14 L13,6 H8 Z"),
            new("Готовые настройки", "M2,1 H12 V13 H2 Z M5,6 L7,8 L10,4"),
            new("GTA V", "M0,0 H5 L8,8 L11,0 H16 L9,13 H7 Z")
        };
        CategoryList.SelectedIndex = 0;
        PopulateSortOptions(0);
        StatusAny.IsChecked = true;
        RiskAny.IsChecked = true;
        HoverVolumeSlider.Value = ((App)Application.Current).Preferences.HoverVolume;
        UpdateLanguagePicker();
        _viewReady = true;
        RefreshFilter();
        UpdateSummary();
    }

    private void PopulateSortOptions(int selectedIndex)
    {
        SortBox.ItemsSource = new[]
        {
            UiLanguage.Text("По порядку", "Catalog order"),
            UiLanguage.Text("По имени ↑", "Name A–Z"),
            UiLanguage.Text("По имени ↓", "Name Z–A")
        };
        SortBox.SelectedIndex = selectedIndex;
    }


    private async void ScanAll_Click(object sender, RoutedEventArgs e) => await ScanAll();
    private async Task ScanAll()
    {
        if (_busy) return; _busy = true; StatusText.Text = UiLanguage.Text("Проверка системы…", "Checking system…");
        foreach (var tweak in _profileItems)
        {
            tweak.State = TweakState.Checking;
            tweak.State = await ScanWindow.Inspect(tweak, CancellationToken.None);
        }
        UpdateSummary();
        StatusText.Text = UiLanguage.Text("Проверка завершена", "Check complete"); _busy = false;
        UpdatePreset();
        ((App)Application.Current).PlaySound(InterfaceSound.ScanComplete);
    }

    private void UpdateSummary()
    {
        _hasScanSummary = true;
        UpdatePreset();
        int applied = _profileItems.Count(tweak => !tweak.ReadOnly && tweak.State == TweakState.Applied);
        int checkedCount = _profileItems.Count(tweak => tweak.State is not (TweakState.Unknown or TweakState.Checking));
        int unavailable = _profileItems.Count(tweak => tweak.State is TweakState.Unsupported or TweakState.Error);
        SummaryText.Text = UiLanguage.Text($"Проверено {checkedCount}/{_profileItems.Count} · применено {applied} · недоступно {unavailable}",
            $"Checked {checkedCount}/{_profileItems.Count} · {applied} applied · {unavailable} unavailable");

        if (_statusFilter.HasValue && !_categoryChanging)
            RefreshFilter(resetScroll: false);
    }

    private async void ApplyOne_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Tweak tweak) await Execute(tweak, true);
    }
    private async void RestoreOne_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as Button)?.Tag is Tweak tweak) await Execute(tweak, false);
    }
    private void OpenPreset_Click(object sender, RoutedEventArgs e) =>
        CategoryList.SelectedItem = CategoryList.Items.Cast<CategoryOption>().Single(x => x.Key == "Готовые настройки");

    private async void ApplySafe_Click(object sender, RoutedEventArgs e)
    {
        if (_busy) return;
        var pending = PresetItems.Where(t => t.CanApply).ToArray();
        if (pending.Length == 0 || !ConfirmAdmin(UiLanguage.Text(
            $"Применить {pending.Length} настроек из игрового набора? Пресет GTA V использует 1920 × 1080 при 239 Гц.",
            $"Apply {pending.Length} settings from the gaming preset? The GTA V preset uses 1920 × 1080 at 239 Hz."),
            pending.Any(t => t.RequiresAdmin))) return;

        foreach (var tweak in pending) await Execute(tweak, true, false);
        UpdatePreset();
        if (pending.All(t => t.State == TweakState.Applied))
            ((App)Application.Current).PlaySound(InterfaceSound.Success);
    }

    private async Task Execute(Tweak tweak, bool apply, bool ask = true)
    {
        if (_busy || !_profileItems.Contains(tweak) || (apply ? !tweak.CanApply : !tweak.CanRestore)) return;
        string action = apply ? UiLanguage.Text("Применить", "Apply") : UiLanguage.Text("Откатить", "Restore");
        if (ask && !ConfirmAdmin($"{action} «{tweak.DisplayTitle}»?\n\n{tweak.DisplayDescription}\n{tweak.Mechanism}", tweak.RequiresAdmin)) return;
        // Serialize backup and settings writes across running instances in this Windows session.
        using var gate = new Mutex(false, @"Local\MagnumTweaker.SettingsWrite");
        bool acquired;
        try { acquired = gate.WaitOne(0); } catch (AbandonedMutexException) { acquired = true; }
        if (!acquired) { StatusText.Text = UiLanguage.Text("Другое окно меняет настройки.", "Another window is changing settings."); return; }
        bool highlight = apply && tweak.State != TweakState.Applied;
        try
        {
            _busy = true; tweak.State = TweakState.Checking; StatusText.Text = $"{action} · {tweak.DisplayTitle}";
            _catalog.ReloadBackups();
            if (apply) await tweak.Apply(CancellationToken.None); else await tweak.Restore(CancellationToken.None);
            tweak.State = await ScanWindow.Inspect(tweak, CancellationToken.None);
            if (apply && tweak.State != TweakState.Applied) throw new InvalidOperationException(UiLanguage.Text("Повторная проверка не подтвердила изменение.", "The read-back did not confirm the change."));
            if (tweak.Id.StartsWith("ac-") || tweak.Id is "ultimate" or "high-performance")
                foreach (var related in _profileItems.Where(x => x != tweak && (x.Id.StartsWith("ac-") || x.Id is "ultimate" or "high-performance")))
                    related.State = await ScanWindow.Inspect(related, CancellationToken.None);
            if (highlight && ask) ((App)Application.Current).PlaySound(InterfaceSound.Success);
            if (highlight && tweak.State == TweakState.Applied && SystemParameters.ClientAreaAnimation)
                _ = HighlightAppliedAsync(tweak);
            StatusText.Text = tweak.RequiresRestart
                ? UiLanguage.Text("Готово. Для полного эффекта нужна перезагрузка.", "Done. Restart Windows to complete the change.")
                : UiLanguage.Text("Готово", "Ready");
        }
        catch (UnauthorizedAccessException)
        {
            MessageBox.Show(UiLanguage.Text("Для этой настройки нужны права администратора. Перезапустите Magnum Tweaker от имени администратора.",
                "This tweak requires administrator rights. Restart Magnum Tweaker as administrator."),
                "Magnum Tweaker", MessageBoxButton.OK, MessageBoxImage.Warning);
            tweak.State = TweakState.Error;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, UiLanguage.Text("Не удалось выполнить действие", "Action failed"), MessageBoxButton.OK, MessageBoxImage.Error);
            tweak.State = TweakState.Error;
        }
        finally { gate.ReleaseMutex(); _busy = false; UpdateSummary(); }
    }

    private static async Task HighlightAppliedAsync(Tweak tweak)
    {
        if (tweak.IsActivationHighlighted) return;
        tweak.IsActivationHighlighted = true;
        await Task.Delay(780);
        tweak.IsActivationHighlighted = false;
    }

    private bool ConfirmAdmin(string text, bool requiresAdmin = true)
    {
        string caption = UiLanguage.Text("Подтверждение", "Confirm");
        if (requiresAdmin && !TweakCatalog.IsAdministrator())
        {
            string prompt = UiLanguage.Text("Некоторым настройкам нужны права администратора. Перезапустить приложение с повышением прав?",
                "Some tweaks require administrator rights. Restart the app as administrator?");
            var result = MessageBox.Show(text + "\n\n" + prompt, caption, MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    string language = UiLanguage.IsEnglish ? "en" : "ru";
                    ((App)Application.Current).Preferences.Save();
                    Process.Start(new ProcessStartInfo(Environment.ProcessPath!, $"--resume-setup {language} {_windowsVersion}")
                    { UseShellExecute = true, Verb = "runas" });
                    Application.Current.Shutdown();
                }
                catch { }
            }
            return false;
        }
        return MessageBox.Show(text, caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    }

    private void RefreshFilter(bool resetScroll = true)
    {
        if (!_viewReady) return;

        string category = (CategoryList.SelectedItem as CategoryOption)?.Key ?? AllTweaks;
        string query = SearchBox.Text.Trim();
        bool preset = category == "Готовые настройки";
        PresetPanel.Visibility = preset ? Visibility.Visible : Visibility.Collapsed;
        TweaksList.Visibility = preset ? Visibility.Collapsed : Visibility.Visible;
        var filtered = _profileItems.Where(x => !preset && (category == AllTweaks || x.Category == category) &&
            (!_statusFilter.HasValue || x.State == _statusFilter.Value) &&
            (!_riskFilter.HasValue || x.Risk == _riskFilter.Value) &&
            (query.Length == 0 || x.DisplayTitle.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                x.DisplayDescription.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                x.PlatformText.Contains(query, StringComparison.CurrentCultureIgnoreCase) ||
                x.Mechanism.Contains(query, StringComparison.CurrentCultureIgnoreCase)));
        var results = (SortBox.SelectedIndex switch
        {
            1 => filtered.OrderBy(x => x.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            2 => filtered.OrderByDescending(x => x.DisplayTitle, StringComparer.CurrentCultureIgnoreCase),
            _ => filtered
        }).ToArray();
        if (resetScroll)
        {
            TweakScroll?.StopWheelAnimation();
            TweakScroll?.ScrollToTop();
        }
        if (!_visible.SequenceEqual(results))
        {
            _visible = results;
            TweaksList.ItemsSource = _visible;
        }
        EmptyText.Visibility = !preset && _visible.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        SectionTitle.Text = UiLanguage.Category(category);
    }

    private void CategoryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady || CategoryList.SelectedIndex < 0) return;

        double markerY = CategoryList.SelectedIndex * 45d;
        int transition = ++_categoryTransition;
        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            CategoryMarker.BeginAnimation(TranslateTransform.YProperty, null);
            CategoryMarker.Y = markerY;
            _categoryChanging = false;
            Results.BeginAnimation(OpacityProperty, null);
            ResultsMotion.BeginAnimation(TranslateTransform.YProperty, null);
            RefreshFilter();
            return;
        }

        // Start at the current animated position so quick category changes do not jump.
        double currentMarkerY = CategoryMarker.Y;
        CategoryMarker.Y = markerY;
        CategoryMarker.BeginAnimation(TranslateTransform.YProperty, EaseTo(currentMarkerY, markerY, 270));

        _categoryChanging = true;
        var fadeOut = new DoubleAnimation(Results.Opacity, 0, TimeSpan.FromMilliseconds(155));
        fadeOut.Completed += (_, _) =>
        {
            if (transition != _categoryTransition) return;
            _categoryChanging = false;
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() =>
            {
                if (transition != _categoryTransition) return;
                RefreshFilter();
                Results.BeginAnimation(OpacityProperty, EaseTo(0, 1, 225));
                ResultsMotion.BeginAnimation(TranslateTransform.YProperty, EaseTo(9, 0, 225));
            }));
        };
        Results.BeginAnimation(OpacityProperty, fadeOut);
    }

    private void RefreshSearch()
    {
        if (!_categoryChanging)
            RefreshFilter();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshSearch();
    private void SearchOptions_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshSearch();

    private void StatusFilter_Checked(object sender, RoutedEventArgs e)
    {
        _statusFilter = Enum.TryParse<TweakState>((string)((RadioButton)sender).Tag, out var status) ? status : null;
        FiltersChanged();
    }

    private void RiskFilter_Checked(object sender, RoutedEventArgs e)
    {
        _riskFilter = Enum.TryParse<TweakRisk>((string)((RadioButton)sender).Tag, out var risk) ? risk : null;
        FiltersChanged();
    }

    private void FiltersChanged()
    {
        if (!_viewReady) return;
        UpdateFilterLabel();
        RefreshSearch();
    }

    private void UpdateFilterLabel()
    {
        int count = (_statusFilter.HasValue ? 1 : 0) + (_riskFilter.HasValue ? 1 : 0);
        FilterLabel.Text = UiLanguage.Text("Фильтры", "Filters") + (count > 0 ? $" · {count}" : "");
    }

    private void ResetFilters_Click(object sender, RoutedEventArgs e)
    {
        StatusAny.IsChecked = true;
        RiskAny.IsChecked = true;
    }

    private void FilterPopup_Opened(object sender, EventArgs e) => StatusAny.Focus();

    private void FilterPopup_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        FilterButton.IsChecked = false;
        FilterButton.Focus();
        e.Handled = true;
    }

    private void Settings_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsOverlay.IsVisible) return;

        BlurCatalog();
        SettingsOverlay.Visibility = Visibility.Visible;
        CloseSettingsButton.Focus();
        if (SystemParameters.ClientAreaAnimation)
        {
            SettingsOverlay.BeginAnimation(OpacityProperty, EaseTo(0, 1, 220));
            SettingsMotion.BeginAnimation(TranslateTransform.YProperty, EaseTo(9, 0, 240));
            SidebarGearRotation.BeginAnimation(RotateTransform.AngleProperty, EaseTo(0, 360, 630));
            SettingsGearRotation.BeginAnimation(RotateTransform.AngleProperty, EaseTo(0, 360, 630));
        }
    }

    private void CloseSettings_Click(object sender, RoutedEventArgs e) => CloseSettings();
    private void SettingsBackdrop_Click(object sender, MouseButtonEventArgs e) => CloseSettings();

    private void SettingsOverlay_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        if (LanguageDropdown.IsChecked == true)
        {
            LanguageDropdown.IsChecked = false;
            LanguageDropdown.Focus();
        }
        else
        {
            CloseSettings();
        }
        e.Handled = true;
    }

    private void CloseSettings()
    {
        if (_settingsClosing) return;

        void Finish()
        {
            SettingsOverlay.Visibility = Visibility.Collapsed;
            SettingsOverlay.BeginAnimation(OpacityProperty, null);
            LanguageDropdown.IsChecked = false;
            RestoreCatalog();
            SettingsButton.Focus();
            _settingsClosing = false;
        }

        if (!SystemParameters.ClientAreaAnimation)
        {
            Finish();
            return;
        }

        _settingsClosing = true;
        var fade = new DoubleAnimation(SettingsOverlay.Opacity, 0, TimeSpan.FromMilliseconds(140));
        fade.Completed += (_, _) => Finish();
        SettingsOverlay.BeginAnimation(OpacityProperty, fade);
    }

    private void UpdateLanguagePicker()
    {
        CurrentLanguage.Text = UiLanguage.Text("Русский", "English");
        CurrentFlag.ContentTemplate = (DataTemplate)FindResource(UiLanguage.Text("RussianFlag", "EnglishFlag"));
        var active = new SolidColorBrush(Color.FromRgb(55, 38, 42));
        EnglishLanguageOption.Background = UiLanguage.IsEnglish ? active : Brushes.Transparent;
        RussianLanguageOption.Background = UiLanguage.IsEnglish ? Brushes.Transparent : active;
    }

    private void LanguageOption_Click(object sender, RoutedEventArgs e)
    {
        var english = (string)((Button)sender).Tag == "en";
        LanguageDropdown.IsChecked = false;
        LanguageDropdown.Focus();
        if (UiLanguage.IsEnglish == english) return;

        ((App)Application.Current).Preferences.English = english;
        UiLanguage.Select(english);
        UpdateLanguagePicker();
        _viewReady = false;
        int category = CategoryList.SelectedIndex;
        CategoryList.Items.Refresh();
        CategoryList.SelectedIndex = category;
        PopulateSortOptions(SortBox.SelectedIndex);
        _viewReady = true;

        foreach (var tweak in _catalog.Items)
            tweak.RefreshLocalizedText();
        UpdateFilterLabel();
        if (_hasScanSummary) UpdateSummary();
        if (!_busy) StatusText.SetResourceReference(TextBlock.TextProperty, "Ready");
        RefreshSearch();
    }

    private void HoverVolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_viewReady)
            ((App)Application.Current).Preferences.HoverVolume = (int)e.NewValue;
    }

    private void BlurCatalog()
    {
        FilterButton.IsChecked = false;
        SortBox.IsDropDownOpen = false;
        TweakScroll?.StopWheelAnimation();
        MainLayout.IsHitTestVisible = false;
        KeyboardNavigation.SetTabNavigation(MainLayout, KeyboardNavigationMode.None);
        var blur = new BlurEffect { Radius = 13, RenderingBias = RenderingBias.Performance };
        MainLayout.Effect = blur;
        if (SystemParameters.ClientAreaAnimation)
            blur.BeginAnimation(BlurEffect.RadiusProperty, EaseTo(0, 13, 220));
    }

    private void RestoreCatalog()
    {
        MainLayout.Effect = null;
        MainLayout.IsHitTestVisible = true;
        KeyboardNavigation.SetTabNavigation(MainLayout, KeyboardNavigationMode.Continue);
    }

    private void Reference_Click(object sender, RoutedEventArgs e)
    {
        if (SettingsOverlay.IsVisible) return;
        var guide = new ReferenceWindow(_profileItems) { Owner = this };
        BlurCatalog();
        try { guide.ShowDialog(); }
        finally
        {
            RestoreCatalog();
            SidebarGuide.Focus();
        }
        if (guide.SelectedTweak is not { } tweak) return;
        CategoryList.SelectedIndex = 0;
        StatusAny.IsChecked = true;
        RiskAny.IsChecked = true;
        _statusFilter = null;
        _riskFilter = null;
        SearchBox.Text = tweak.DisplayTitle;
        UpdateFilterLabel();
        RefreshFilter();
    }

    private void ProfilePicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_viewReady) return;
        string requested = ProfilePicker.SelectedIndex == 1 ? "11" : "10";
        if (requested == _windowsVersion) return;
        var items = _catalog.ForProfile(requested);
        if (!_busy && new ScanWindow(items) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog() == true)
        {
            _windowsVersion = requested;
            ((App)Application.Current).Preferences.WindowsProfile = requested;
            _profileItems = items;
            ProfileText.Text = $"Windows {requested} · x64";
            UpdateSummary();
            RefreshFilter();
        }
        else ProfilePicker.SelectedIndex = _windowsVersion == "11" ? 1 : 0;
    }

    private static DoubleAnimation EaseTo(double from, double to, double milliseconds) =>
        new(from, to, TimeSpan.FromMilliseconds(milliseconds))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };

    private void ViewTerms_Click(object sender, RoutedEventArgs e) =>
        new TermsWindow(requireAcceptance: false) { Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner }.ShowDialog();

    private void Minimize_Click(object sender, RoutedEventArgs e) => SystemCommands.MinimizeWindow(this);

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        (PresentationSource.FromVisual(this) as HwndSource)?.AddHook(HandleWindowMessage);
    }

    private IntPtr HandleWindowMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        const int systemCommand = 0x0112;
        const int minimizeCommand = 0xF020;
        if (message == systemCommand && (wParam.ToInt64() & 0xFFF0) == minimizeCommand &&
            IsLoaded && WindowState != WindowState.Minimized && SystemParameters.ClientAreaAnimation)
        {
            handled = true;
            AnimateMinimize();
        }
        return IntPtr.Zero;
    }

    private void AnimateMinimize()
    {
        if (_minimizing) return;
        _minimizing = true;
        static DoubleAnimation Transition(double from, double to)
        {
            var animation = EaseTo(from, to, 280);
            animation.FillBehavior = FillBehavior.HoldEnd;
            return animation;
        }

        MinimizeScale.BeginAnimation(ScaleTransform.ScaleXProperty, Transition(1, 0.9));
        MinimizeScale.BeginAnimation(ScaleTransform.ScaleYProperty, Transition(1, 0.9));
        MinimizeMotion.BeginAnimation(TranslateTransform.YProperty, Transition(0, 22));
        var fade = Transition(1, 0.20);
        fade.Completed += (_, _) =>
        {
            if (!IsLoaded || !_minimizing) return;
            WindowState = WindowState.Minimized;
            ResetMinimizeAnimation();
        };
        WindowFrame.BeginAnimation(OpacityProperty, fade);
    }

    private void ResetMinimizeAnimation()
    {
        _minimizing = false;
        MinimizeScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        MinimizeScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        MinimizeMotion.BeginAnimation(TranslateTransform.YProperty, null);
        WindowFrame.BeginAnimation(OpacityProperty, null);
    }
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized) SystemCommands.RestoreWindow(this);
        else SystemCommands.MaximizeWindow(this);
    }
    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (MaximizeGlyph is not null)
            MaximizeGlyph.Data = Geometry.Parse(WindowState == WindowState.Maximized
                ? "M 1,4 H 9 V 12 H 1 Z M 4,4 V 1 H 12 V 9 H 9"
                : "M 1,1 H 11 V 11 H 1 Z");
        if (IsLoaded && WindowState == WindowState.Maximized && SystemParameters.ClientAreaAnimation)
        {
            WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, EaseTo(0.94, 1, 530));
            WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, EaseTo(0.94, 1, 530));
            MainLayout.BeginAnimation(OpacityProperty, EaseTo(0.65, 1, 430));
        }
        else if (WindowScale is not null)
        {
            WindowScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            WindowScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        }
    }
    private void Close_Click(object sender, RoutedEventArgs e) => SystemCommands.CloseWindow(this);
}
