using System.IO;
using System.Text.Json;

namespace MagnumTweaker.Services;

internal sealed class InterfacePreferences
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagnumTweaker", "interface.json");

    public bool English { get; set; }
    public string? GamePath { get; set; }
    public string? WindowsProfile { get; set; }
    public int HoverVolume { get; set; } = 10;
    public int AcceptedTermsVersion { get; set; }
    public bool RestorePointPromptHandled { get; set; }

    public static InterfacePreferences Load() => Load(FilePath);

    internal static InterfacePreferences Load(string path)
    {
        try
        {
            var settings = JsonSerializer.Deserialize<InterfacePreferences>(File.ReadAllText(path));
            if (settings is not null)
            {
                settings.HoverVolume = Math.Clamp(settings.HoverVolume, 0, 100);
                return settings;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }

        return new();
    }

    public void Save() => Save(FilePath);

    internal void Save(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(this));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
