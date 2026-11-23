using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace MagnumTweaker;

public sealed class SmoothScrollViewer : ScrollViewer
{
    public static readonly DependencyProperty ShowTopFadeProperty = DependencyProperty.Register(
        nameof(ShowTopFade), typeof(bool), typeof(SmoothScrollViewer), new PropertyMetadata(true, OnFadeSettingChanged));

    private static readonly DependencyPropertyKey TopFadeOpacityPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(TopFadeOpacity), typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(0d));

    private static readonly DependencyPropertyKey BottomFadeOpacityPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(BottomFadeOpacity), typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(0d));

    public static readonly DependencyProperty TopFadeOpacityProperty = TopFadeOpacityPropertyKey.DependencyProperty;
    public static readonly DependencyProperty BottomFadeOpacityProperty = BottomFadeOpacityPropertyKey.DependencyProperty;

    private static readonly DependencyProperty AnimatedOffsetProperty = DependencyProperty.Register(
        "AnimatedOffset", typeof(double), typeof(SmoothScrollViewer), new PropertyMetadata(0d, OnAnimatedOffsetChanged));

    private double? _wheelTarget;

    public bool ShowTopFade
    {
        get => (bool)GetValue(ShowTopFadeProperty);
        set => SetValue(ShowTopFadeProperty, value);
    }

    public double TopFadeOpacity => (double)GetValue(TopFadeOpacityProperty);
    public double BottomFadeOpacity => (double)GetValue(BottomFadeOpacityProperty);

    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        base.OnPreviewMouseWheel(e);
        int lines = SystemParameters.WheelScrollLines;
        if (e.Handled || !SystemParameters.ClientAreaAnimation || lines == 0 || ScrollableHeight <= 0)
            return;

        e.Handled = true;
        double step = lines < 0 ? ViewportHeight : lines * 24d;
        double target = Math.Clamp((_wheelTarget ?? VerticalOffset) - step * e.Delta / 120d, 0, ScrollableHeight);
        if (target == (_wheelTarget ?? VerticalOffset)) return;
        _wheelTarget = target;
        var animation = new DoubleAnimation(VerticalOffset, target, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
        };
        animation.Completed += (_, _) => StopWheelAnimation();
        BeginAnimation(AnimatedOffsetProperty, animation);
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        StopWheelAnimation();
        base.OnPreviewMouseDown(e);
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        StopWheelAnimation();
        base.OnPreviewKeyDown(e);
    }

    protected override void OnScrollChanged(ScrollChangedEventArgs e)
    {
        UpdateFades();
        base.OnScrollChanged(e);
    }

    public void StopWheelAnimation()
    {
        if (!_wheelTarget.HasValue) return;
        _wheelTarget = null;
        BeginAnimation(AnimatedOffsetProperty, null);
    }

    private void UpdateFades()
    {
        const double distance = 24;
        SetValue(TopFadeOpacityPropertyKey, ShowTopFade ? Math.Clamp(VerticalOffset / distance, 0, 1) : 0);
        SetValue(BottomFadeOpacityPropertyKey, Math.Clamp((ScrollableHeight - VerticalOffset) / distance, 0, 1));
    }

    private static void OnFadeSettingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        => ((SmoothScrollViewer)sender).UpdateFades();

    private static void OnAnimatedOffsetChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
        var viewer = (SmoothScrollViewer)sender;
        double value = (double)e.NewValue;
        double offset = viewer._wheelTarget == value ? value : Math.Clamp(Math.Round(value), 0, viewer.ScrollableHeight);
        if (viewer._wheelTarget.HasValue && offset != viewer.VerticalOffset)
            viewer.ScrollToVerticalOffset(offset);
    }
}
