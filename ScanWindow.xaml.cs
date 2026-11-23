using System.Windows;
using MagnumTweaker.Models;

namespace MagnumTweaker;

public partial class ScanWindow : Window
{
    private readonly IReadOnlyList<Tweak> _items;
    private readonly CancellationTokenSource _closing = new();
    private bool _started;
    internal ScanWindow(IReadOnlyList<Tweak> items)
    {
        _items = items;
        InitializeComponent();
        Title = Heading.Text = UiLanguage.Text("Проверка настроек Windows", "Checking Windows settings");
        Progress.Maximum = items.Count;
        Counter.Text = $"0 / {items.Count}";
        WindowTransitions.PrepareEntrance(this);
    }

    internal static async Task<TweakState> Inspect(Tweak tweak, CancellationToken ct)
    {
        string host = Environment.OSVersion.Version.Build >= 22000 ? "11" : "10";
        if (tweak.WindowsProfile is not null && tweak.WindowsProfile != host) return TweakState.Unsupported;
        if (tweak.Id == "delivery-background-limit" && Environment.OSVersion.Version.Build < 17134) return TweakState.Unsupported;
        if (tweak.Id == "widgets-policy")
        {
            using var version = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (version?.GetValue("EditionID") is not string edition ||
                !new[] { "Professional", "Enterprise", "Education", "IoTEnterprise" }.Any(prefix => edition.StartsWith(prefix, StringComparison.Ordinal)))
                return TweakState.Unsupported;
        }
        if (tweak.Id == "hags" && Environment.OSVersion.Version.Build < 19041) return TweakState.Unsupported;
        try { return await Task.Run(() => tweak.Check(ct), ct); }
        catch (NotSupportedException) { return TweakState.Unsupported; }
        catch (Exception) when (ct.IsCancellationRequested) { throw new OperationCanceledException(ct); }
        catch (Exception) when (!ct.IsCancellationRequested) { return TweakState.Error; }
    }

    protected override void OnClosed(EventArgs e)
    {
        _closing.Cancel();
        base.OnClosed(e);
    }

    private async void StartScan(object? sender, EventArgs e)
    {
        if (_started) return;
        _started = true;
        try
        {
            int done = 0;
            foreach (var tweak in _items)
            {
                _closing.Token.ThrowIfCancellationRequested();
                CurrentItem.Text = tweak.DisplayTitle;
                tweak.State = await Inspect(tweak, _closing.Token);
                Progress.Value = ++done;
                Counter.Text = $"{done} / {_items.Count}";
                await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);
            }
            CurrentItem.Text = UiLanguage.Text("Проверка завершена", "Check complete");
            await WindowTransitions.FadeOutAsync(this);
            if (IsVisible) DialogResult = true;
        }
        catch (OperationCanceledException) when (_closing.IsCancellationRequested) { }
    }
}
