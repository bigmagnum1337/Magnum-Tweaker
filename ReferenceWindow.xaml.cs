using System.Diagnostics;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using MagnumTweaker.Services;
using MagnumTweaker.Models;

namespace MagnumTweaker;

public partial class ReferenceWindow : Window
{
    private sealed record Entry(string Source, int Id, string Title, string Detail, bool Rp)
    {
        public Tweak? Action { get; set; }
        public Visibility ActionVisibility => Action is null ? Visibility.Collapsed : Visibility.Visible;
        public string DisplaySource => Source.Split(" · ", 2)[0];
        public string SearchMetadata => string.Join(" · ", new[] {Source, Id > 0 ? $"#{Id}" : null,
            Id is 44 or 45 or 46 or 55 ? "NVIDIA" : Id is 47 or 48 or 56 or 199 or 200 ? "AMD" : Id is 49 or 201 ? "Intel" : null,
            Rp ? "Majestic / GTA5RP" : null}.Where(x => x != null));
        public string? SettingsPage => Id switch
        {
            20 or 21 => "gaming-gamemode", 22 or 23 => "gaming-gamedvr",
            25 or 26 or 29 or 30 => "display-advancedgraphics", 41 => "display-advanced",
            31 or 32 or 33 => "easeofaccess-visualeffects", 93 or 169 => "startupapps",
            95 => "search", >= 130 and <= 156 => "appsfeatures", 160 or 161 => "windowsupdate",
            52 or 172 => "storagesense", 84 => "network-status", 92 => "network-vpn",
            _ => Source == "Еженедельный BAT" ? "storagesense" : null
        };
        public Visibility LinkVisibility => SettingsPage == null ? Visibility.Collapsed : Visibility.Visible;
    }

    private readonly Entry[] _entries;
    public Tweak? SelectedTweak { get; private set; }
    public ReferenceWindow(IReadOnlyList<Tweak>? tweaks = null)
    {
        using var data = typeof(ReferenceWindow).Assembly.GetManifestResourceStream("MagnumTweaker.Reference.json")!;
        _entries = JsonSerializer.Deserialize<Entry[]>(data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var byId = tweaks?.ToDictionary(tweak => tweak.Id, StringComparer.Ordinal);
        foreach (var entry in _entries)
        {
            string? id = entry.Id switch {
                1=>"high-performance",2=>"ultimate",17=>"power-throttling",20=>"game-mode",22=>"game-dvr",23=>"game-config",
                24=>"game-dvr-policy",25=>"hags",28=>"game-exe-fso",30=>"game-exe-gpu",31=>"transparency",32=>"taskbar-motion",
                33=>"window-animations",34=>"visual-effects",36=>"tips",38=>"search-highlights",39=>"uwp10",40=>"start-suggestions",
                57=>"mouse-accel",58=>"mouse-threshold1",59=>"mouse-threshold2",66=>"controller-bar",68=>"tcp-rss",69=>"tcp-autotuning",
                96=>"rp-WSearch",97=>"rp-SysMain",98=>"rp-DiagTrack",100=>"svc-MapsBroker",101=>"svc-Spooler",102=>"svc-Fax",
                103=>"svc-bthserv",104=>"svc-XblAuthManager",105=>"svc-XblGameSave",106=>"svc-XboxNetApiSvc",107=>"svc-XboxGipSvc",
                108=>"svc-TabletInputService",110=>"svc-WMPNetworkSvc",112=>"svc-RetailDemo",113=>"svc-lfsvc",116=>"svc-wisvc",
                117=>"svc-PhoneSvc",118=>"svc-StiSvc",119=>"svc-icssvc",120=>"svc-CscService",121=>"svc-TrkWks",
                158=>"delivery-p2p",169=>"startup-delay",174=>"mmcss-reserve",177=>"mmcss-priority",178=>"mmcss-games",179=>"cpu-quantum",
                _=>null };
            if (id != null && byId != null) entry.Action = byId.GetValueOrDefault(id);
        }
        InitializeComponent();
        Sources.ItemsSource = new[] { UiLanguage.Text("Все источники", "All sources") }.Concat(_entries.Select(x => x.DisplaySource).Distinct());
        Sources.SelectedIndex = 0;
        if (SystemParameters.ClientAreaAnimation)
        {
            GuideMotion.Y = 4;
            Loaded += (_, _) => GuideMotion.BeginAnimation(System.Windows.Media.TranslateTransform.YProperty,
                new DoubleAnimation(4, 0, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                });
        }
    }

    private void Search_MouseEnter(object sender, MouseEventArgs e) =>
        ((App)Application.Current).PlaySound(InterfaceSound.Hover);

    private void FilterChanged(object sender, RoutedEventArgs e)
    {
        if (Entries == null || Sources.SelectedIndex < 0) return;
        var query = Search.Text.Trim();
        var results = _entries.Where(x => (Sources.SelectedIndex == 0 || x.DisplaySource == Sources.SelectedItem as string) &&
            (x.SearchMetadata + " " + x.Title + " " + x.Detail).Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        Entries.ItemsSource = results;
        CountLabel.Text = $"{results.Length} / {_entries.Length}";
        (Entries.Template?.FindName("GuideScroller", Entries) as SmoothScrollViewer)?.ScrollToTop();
    }

    private void OpenSetting_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string page }) return;
        try { Process.Start(new ProcessStartInfo("ms-settings:" + page) { UseShellExecute = true }); }
        catch (System.ComponentModel.Win32Exception ex) { MessageBox.Show(this, ex.Message, Title); }
    }

    private async void Close_Click(object sender, RoutedEventArgs e)
    {
        IsEnabled=false;
        await WindowTransitions.FadeOutAsync(this);
        if (IsVisible) Close();
    }

    private void ShowTweak_Click(object sender, RoutedEventArgs e)
    {
        SelectedTweak=(sender as Button)?.Tag as Tweak;
        Close_Click(sender,e);
    }
}
