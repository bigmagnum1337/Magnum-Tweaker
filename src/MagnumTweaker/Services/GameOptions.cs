using System.IO;
using MagnumTweaker.Models;
using Microsoft.Win32;

namespace MagnumTweaker.Services;

internal static class GameOptions
{
    public static IEnumerable<Tweak> Create(string path, BackupStore backups)
    {
        if (!Path.IsPathFullyQualified(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) yield break;
        string name = Path.GetFileName(path);
        var options = new (string Id,string Key,string Field,string Desired,char Separator,string Ru,string En,string Detail,string English)[]
        {
            ("gpu", @"Software\Microsoft\DirectX\UserGpuPreferences", "GpuPreference=", "GpuPreference=2", ';',
             "Производительная видеокарта", "High-performance GPU",
             "Сохраняет предпочтение Windows для этой игры. На ПК с несколькими GPU Windows предложит производительный адаптер при следующем запуске.",
             "Saves the Windows GPU preference for this game. On multi-GPU systems Windows will prefer the high-performance adapter on the next launch."),
            ("fso", @"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers", "DISABLEDXMAXIMIZEDWINDOWEDMODE", "DISABLEDXMAXIMIZEDWINDOWEDMODE", ' ',
             "Отключить полноэкранную оптимизацию", "Disable fullscreen optimizations",
             "Устанавливает флаг совместимости выбранного EXE. Перезапустите игру и сравните время кадра в полноэкранном режиме.",
             "Sets the compatibility flag for this executable. Restart the game and compare fullscreen frame times.")
        };
        foreach (var option in options)
        {
            string id = "game-exe-" + option.Id;
            string key = $"{id}:{path.ToUpperInvariant()}";
            bool Matches(string token) => option.Separator == ';' ? token.StartsWith(option.Field, StringComparison.OrdinalIgnoreCase)
                : token.Equals(option.Field,StringComparison.OrdinalIgnoreCase);
            string? Read()
            {
                using var reg = Registry.CurrentUser.OpenSubKey(option.Key);
                var value = reg?.GetValue(path, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
                return value is null or string ? (string?)value : throw new InvalidDataException(path);
            }
            string[] Parts(string? value) => (value ?? "").Split(option.Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            string? Field(string? raw) => Parts(raw).SingleOrDefault(Matches);
            void Write(string? field, bool hadValue)
            {
                var parts = Parts(Read()).Where(x => !Matches(x)).ToList();
                if (field != null) parts.Add(field);
                using var reg = Registry.CurrentUser.CreateSubKey(option.Key, true);
                if (parts.Count == 0 && !hadValue) reg.DeleteValue(path,false);
                else reg.SetValue(path,string.Join(option.Separator,parts) + (option.Separator == ';' && parts.Count > 0 ? ";" : ""),RegistryValueKind.String);
                if (Field(Read()) != field) throw new IOException(path);
            }
            yield return new Tweak {
                Id=id, Title=$"{option.Ru} · {name}", EnglishTitle=$"{option.En} · {name}", Category=name.StartsWith("GTA5", StringComparison.OrdinalIgnoreCase) ? "GTA V" : "FPS и игры", Impact="GPU",
                Description=option.Detail, EnglishDescription=option.English, Risk=TweakRisk.Experimental,
                RequiresAdmin=false,
                Hardware=option.Id=="gpu" ? "Multi-GPU" : "DirectX", Mechanism=$"HKCU\\{option.Key} · {path} · {option.Desired}",
                HasBackup=()=>backups.Contains(key),
                Check=_=>Task.FromResult(Field(Read()) == option.Desired ? TweakState.Applied : TweakState.NotApplied),
                Apply=_=>{
                    if (!File.Exists(path)) throw new FileNotFoundException(name);
                    var raw=Read();
                    if (Field(raw) != option.Desired) {
                        backups.Save(key,Field(raw),"String",raw!=null);
                        Write(option.Desired,true);
                    }
                    return Task.CompletedTask;
                },
                Restore=_=>{
                    var original=backups.Get(key) ?? throw new InvalidOperationException(name);
                    Write(original.Value,original.Existed);
                    backups.Forget(key);
                    return Task.CompletedTask;
                }
            };
        }
    }
}
