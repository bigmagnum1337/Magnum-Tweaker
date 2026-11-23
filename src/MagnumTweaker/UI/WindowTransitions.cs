using System.Windows;
using System.Windows.Media.Animation;

namespace MagnumTweaker;

internal static class WindowTransitions
{
    public static void PrepareEntrance(Window window)
    {
        if (!SystemParameters.ClientAreaAnimation) return;

        window.Opacity = 0;
        window.Loaded += Show;

        void Show(object sender, RoutedEventArgs e)
        {
            window.Loaded -= Show;
            window.Opacity = 1;
            var animation = CreateFade(0, 1, 240);
            animation.FillBehavior = FillBehavior.Stop;
            window.BeginAnimation(UIElement.OpacityProperty, animation);
        }
    }

    public static Task FadeOutAsync(Window window)
    {
        if (!SystemParameters.ClientAreaAnimation || !window.IsVisible)
            return Task.CompletedTask;

        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var animation = CreateFade(window.Opacity, 0, 145);
        animation.Completed += (_, _) => completed.TrySetResult();
        window.BeginAnimation(UIElement.OpacityProperty, animation);
        return completed.Task;
    }

    private static DoubleAnimation CreateFade(double from, double to, double duration) =>
        new(from, to, TimeSpan.FromMilliseconds(duration))
        {
            EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
        };
}
