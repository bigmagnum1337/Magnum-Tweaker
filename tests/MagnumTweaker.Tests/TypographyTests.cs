using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Media;

namespace MagnumTweaker.Tests;

public sealed class TypographyTests
{
    [Fact]
    public void AppFontsUsePackagedWeightsAndRussianGlyphs()
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                Check((FontFamily)app.Resources["InterfaceFont"], FontWeights.Normal, "inter-400.ttf");
                Check((FontFamily)app.Resources["InterfaceFont"], FontWeights.SemiBold, "inter-600.ttf");
                Check((FontFamily)app.Resources["InterfaceFont"], FontWeights.Bold, "inter-700.ttf");
                Check((FontFamily)app.Resources["HeadingFont"], FontWeights.Bold, "manrope-700.ttf");
                Check((FontFamily)app.Resources["HeadingFont"], FontWeights.ExtraBold, "manrope-800.ttf");
            }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "WPF font resolution timed out.");
        failure?.Throw();
    }

    private static void Check(FontFamily family, FontWeight weight, string file)
    {
        var typeface = new Typeface(family, FontStyles.Normal, weight, FontStretches.Normal);
        Assert.True(typeface.TryGetGlyphTypeface(out var glyphs));
        Assert.EndsWith(file, glyphs.FontUri.AbsoluteUri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(weight, glyphs.Weight);
        foreach (char letter in "Magnum АБВЖЩЁёжя")
            Assert.True(glyphs.CharacterToGlyphMap.ContainsKey(letter), $"Missing glyph {letter}");
    }
}
