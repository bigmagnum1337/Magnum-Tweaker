using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

internal static class GtaGraphics
{
    public static Tweak Create(BackupStore backups, string path) => Create(backups, path, IsGameRunning);

    internal static Tweak Create(BackupStore backups, string path, Func<bool> isGameRunning)
    {
        string key = "gta-legacy-preset:" + path;
        XDocument Parse(Stream stream)
        {
            using var reader = XmlReader.Create(stream, new XmlReaderSettings {
                DtdProcessing = DtdProcessing.Prohibit, MaxCharactersInDocument = 2_000_000
            });
            return XDocument.Load(reader);
        }
        XDocument Preset(byte[] original)
        {
            using var input = new MemoryStream(original);
            var gpu = Parse(input).Element("Settings")?.Element("VideoCardDescription")?.Value;
            if (string.IsNullOrWhiteSpace(gpu)) throw new NotSupportedException("VideoCardDescription");
            using var source = typeof(GtaGraphics).Assembly.GetManifestResourceStream("MagnumTweaker.GtaPreset.xml")!;
            var document = Parse(source);
            // Read the adapter name from the user's existing profile so the embedded preset stays portable.
            document.Root!.Element("VideoCardDescription")!.Value = gpu;
            return document;
        }
        byte[] Read()
        {
            if (!File.Exists(path)) throw new NotSupportedException(path);
            if (new FileInfo(path).Length > 2_000_000) throw new InvalidDataException(path);
            return File.ReadAllBytes(path);
        }
        void Replace(byte[] bytes)
        {
            if (isGameRunning()) throw new IOException(UiLanguage.Text("Сначала закройте GTA V.", "Close GTA V first."));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try { File.WriteAllBytes(temporary, bytes); File.Replace(temporary, path, null); }
            finally { File.Delete(temporary); }
        }
        bool Matches(byte[] existing, XDocument preset)
        {
            using var stream = new MemoryStream(existing);
            return XNode.DeepEquals(Parse(stream).Root, preset.Root);
        }
        return new Tweak {
            Id = "gta-preset", Category = "GTA V", Title = "GTA V Settings",
            EnglishTitle = "GTA V Settings",
            Description = "Подбирает детализацию и эффекты для баланса нагрузки CPU и GPU. Пресет задаёт 1920 × 1080 и 239 Гц и автоматически переносит название вашей видеокарты из текущего settings.xml. Откат возвращает исходный файл.",
            EnglishDescription = "Adjusts detail and effects to balance CPU and GPU load. The preset sets 1920 × 1080 at 239 Hz and retains your GPU name. Restore recovers the original file.",
            Hardware = "GTA V Legacy", Impact = "GPU", Mechanism = path,
            Risk = TweakRisk.Moderate, RequiresAdmin = false,
            HasBackup = () => backups.Contains(key),
            Check = _ => {
                var existing = Read();
                return Task.FromResult(Matches(existing, Preset(existing)) ? TweakState.Applied : TweakState.NotApplied);
            },
            Apply = _ => {
                var existing = Read();
                var preset = Preset(existing);
                if (Matches(existing, preset)) return Task.CompletedTask;
                backups.Save(key, Convert.ToBase64String(existing), "Base64", true);
                using var output = new MemoryStream();
                preset.Save(output);
                Replace(output.ToArray());
                return Task.CompletedTask;
            },
            Restore = _ => {
                var saved = backups.Get(key)?.Value ?? throw new InvalidOperationException(key);
                Replace(Convert.FromBase64String(saved));
                backups.Forget(key);
                return Task.CompletedTask;
            }
        };
    }

    private static bool IsGameRunning()
    {
        var processes = Process.GetProcessesByName("GTA5");
        foreach (var process in processes) process.Dispose();
        return processes.Length != 0;
    }
}
