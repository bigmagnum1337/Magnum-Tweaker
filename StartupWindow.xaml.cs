using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace MagnumTweaker;

public partial class StartupWindow : Window
{
    public StartupWindow()
    {
        InitializeComponent();
        if (!SystemParameters.ClientAreaAnimation) return;
        LeftMotion.X = 49;
        RightMotion.X = -49;
        Middle.Opacity = Subtitle.Opacity = 0;
        Loaded += (_, _) =>
        {
            LeftMotion.BeginAnimation(TranslateTransform.XProperty, Reveal(0, 140, 425));
            RightMotion.BeginAnimation(TranslateTransform.XProperty, Reveal(0, 140, 425));
            Middle.BeginAnimation(OpacityProperty, Reveal(1, 265, 320));
            Subtitle.BeginAnimation(OpacityProperty, Reveal(1, 555, 260));
        };
    }

    private static DoubleAnimation Reveal(double value, int start, int duration) => new(value, TimeSpan.FromMilliseconds(duration))
    {
        BeginTime = TimeSpan.FromMilliseconds(start),
        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
    };
}
