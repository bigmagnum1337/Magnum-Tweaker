using Microsoft.Win32;
using MagnumTweaker.Models;

namespace MagnumTweaker.Services;

public sealed class RegistryTweakFactory(BackupStore backups)
{
    public Tweak Create(string id, string title, string description, string category, string impact, TweakRisk risk,
        bool restart, RegistryHive hive, string path, string name, object desired, RegistryValueKind kind, string? source = null)
    {
        string backupKey = $"{hive}\\{path}\\{name}";
        return new Tweak
        {
            Id = id, Title = title, Description = description, Category = category, Impact = impact,
            Risk = risk, RequiresRestart = restart, SourceUrl = source,
            RequiresAdmin = hive != RegistryHive.CurrentUser,
            Mechanism = $"{backupKey} = {desired} ({kind})",
            HasBackup = () => backups.Contains(backupKey),
            Check = _ => Task.FromResult(CheckValue(hive, path, name, desired, kind)),
            Apply = _ => { ApplyValue(backupKey, hive, path, name, desired, kind); return Task.CompletedTask; },
            Restore = _ => { RestoreValue(backupKey, hive, path, name); return Task.CompletedTask; }
        };
    }

    private static TweakState CheckValue(RegistryHive hive, string path, string name, object desired, RegistryValueKind kind)
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
            using var key = baseKey.OpenSubKey(path);
            var actual = key?.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
            return actual is not null && key!.GetValueKind(name) == kind && string.Equals(actual.ToString(), desired.ToString(), StringComparison.OrdinalIgnoreCase)
                ? TweakState.Applied : TweakState.NotApplied;
        }
        catch { return TweakState.Error; }
    }

    private void ApplyValue(string backupKey, RegistryHive hive, string path, string name, object desired, RegistryValueKind kind)
    {
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(path, true);
        var original = key.GetValue(name, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        if (original != null && key.GetValueKind(name) == kind && Equals(original, desired)) return;
        if (original is not null && original is not (int or long or string))
            throw new InvalidOperationException(UiLanguage.Text("Неожиданный тип исходного значения.", "Unexpected original value type."));
        backups.Save(backupKey, original, original is null ? null : key.GetValueKind(name).ToString(), original is not null);
        key.SetValue(name, desired, kind);
    }

    private void RestoreValue(string backupKey, RegistryHive hive, string path, string name)
    {
        var backup = backups.Get(backupKey);
        if (backup is null) throw new InvalidOperationException(UiLanguage.Text("Для этой настройки ещё нет сохранённого значения.", "No saved value for this tweak."));
        using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
        using var key = baseKey.CreateSubKey(path, true);
        if (!backup.Existed) { key.DeleteValue(name, false); backups.Forget(backupKey); return; }
        if (!Enum.TryParse<RegistryValueKind>(backup.Kind, out var kind)) throw new System.IO.InvalidDataException(backupKey);
        object value = kind switch
        {
            RegistryValueKind.DWord => int.Parse(backup.Value!),
            RegistryValueKind.QWord => long.Parse(backup.Value!),
            RegistryValueKind.String or RegistryValueKind.ExpandString => backup.Value ?? "",
            _ => throw new System.IO.InvalidDataException(backupKey)
        };
        key.SetValue(name, value, kind);
        if (CheckValue(hive, path, name, value, kind) != TweakState.Applied) throw new System.IO.IOException(backupKey);
        backups.Forget(backupKey);
    }
}
