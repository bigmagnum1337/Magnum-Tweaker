using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MagnumTweaker.Tests;

public sealed class PackagedUiTests
{
    [Fact]
    public void ApplicationAndWindowsLoadPackagedResources()
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
                CheckWindowArtwork(app);
            }
            catch (Exception ex) { failure = ExceptionDispatchInfo.Capture(ex); }
        }) { IsBackground = true };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(10)), "WPF resource loading timed out.");
        failure?.Throw();
    }

    private static void CheckWindowArtwork(App app)
    {
        using var workspace = new TemporaryWorkspace();
        UiLanguage.Select(english: true);
        Window[] windowsWithIcons =
        [
            new MainWindow("11", new TweakCatalog(workspace.Backups)),
            new SetupWindow(english: true),
            new ScanWindow([]),
            new TermsWindow(requireAcceptance: false),
            new ReferenceWindow()
        ];
        foreach (var window in windowsWithIcons)
            AssertHasPixels(window.Icon);

        var startup = new StartupWindow();
        var logoImages = FindImages(startup).ToArray();
        Assert.Equal(4, logoImages.Length);
        Assert.All(logoImages, image => AssertHasPixels(image.Source));

        var brand = (DataTemplate)app.Resources["BrandMark"];
        AssertHasPixels(Assert.IsType<Image>(brand.LoadContent()).Source);
    }

    private static IEnumerable<Image> FindImages(DependencyObject element)
    {
        if (element is Image image) yield return image;
        foreach (object child in LogicalTreeHelper.GetChildren(element))
            if (child is DependencyObject dependency)
                foreach (var descendant in FindImages(dependency))
                    yield return descendant;
    }

    private static void AssertHasPixels(ImageSource? source)
    {
        var bitmap = Assert.IsAssignableFrom<BitmapSource>(source);
        Assert.True(bitmap.PixelWidth > 0);
        Assert.True(bitmap.PixelHeight > 0);
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
