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

    public static InterfacePreferences Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<InterfacePreferences>(File.ReadAllText(FilePath));
            if (settings is not null)
            {
                settings.HoverVolume = Math.Clamp(settings.HoverVolume, 0, 100);
                return settings;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }

        return new();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }
}
