using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MagnumTweaker;

public partial class SetupWindow : Window
{
    private bool _leaving;
    public bool English { get; private set; }
    public string WindowsVersion { get; private set; } = "";

    private readonly bool _preferEleven;
    public SetupWindow(bool english, string? profile = null)
    {
        _preferEleven = profile == "11" || profile is null && Environment.OSVersion.Version.Build >= 22000;
        English = english;
        InitializeComponent();
        WindowTransitions.PrepareEntrance(this);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => ShowStep(false, animate: false);
    private void Russian_Click(object sender, RoutedEventArgs e) => SelectLanguage(false);
    private void English_Click(object sender, RoutedEventArgs e) => SelectLanguage(true);
    private void Back_Click(object sender, RoutedEventArgs e) => ShowStep(false);
    private async void Close_Click(object sender, RoutedEventArgs e) => await ExitAsync(false);
    private void DragHeader(object sender, MouseButtonEventArgs e) => DragMove();

    private void SelectLanguage(bool english)
    {
        if (_leaving) return;
        English = english;
        WindowsTitle.Text = english ? "Choose your Windows version" : "Выберите версию Windows";
        BackLabel.Text = english ? "Back" : "Назад";
        WindowsHint.Text = english ? "You can change this later in Settings." : "Профиль можно изменить в настройках.";
        ShowStep(true);
    }

    private void ShowStep(bool chooseWindows, bool animate = true)
    {
        if (_leaving) return;
        LanguagePage.Visibility = chooseWindows ? Visibility.Collapsed : Visibility.Visible;
        WindowsPage.Visibility = chooseWindows ? Visibility.Visible : Visibility.Collapsed;
        (chooseWindows ? (_preferEleven ? Windows11Button : Windows10Button) : English ? EnglishButton : RussianButton).Focus();
        if (animate && SystemParameters.ClientAreaAnimation)
            StepContent.BeginAnimation(OpacityProperty, new DoubleAnimation(0.35, 1, TimeSpan.FromMilliseconds(160)));
    }

    private async void Windows_Click(object sender, RoutedEventArgs e)
    {
        if (_leaving) return;
        WindowsVersion = (string)((Button)sender).Tag;
        await ExitAsync(true);
    }

    private async Task ExitAsync(bool confirmed)
    {
        if (_leaving) return;
        _leaving = true;
        await WindowTransitions.FadeOutAsync(this);
        if (IsVisible) DialogResult = confirmed;
    }
}
