using System.Text.Json;
using System.IO;

namespace MagnumTweaker.Services;

public sealed class BackupStore
{
    private readonly string _path;
    private Dictionary<string, BackupValue> _values = new();

    public BackupStore() : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MagnumTweaker")) { }

    internal BackupStore(string root)
    {
        Directory.CreateDirectory(root);
        _path = Path.Combine(root, "backup.json");
        Reload();
    }

    internal void Reload()
    {
        try
        {
            _values = File.Exists(_path) ? JsonSerializer.Deserialize<Dictionary<string, BackupValue>>(File.ReadAllText(_path))
                ?? throw new JsonException("backup.json") : new();
            _loadError = null;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _loadError = ex;
            _values = new();
        }
    }
    private Exception? _loadError;

    public bool Contains(string key) => _values.ContainsKey(key);
    public BackupValue? Get(string key) => _values.GetValueOrDefault(key);

    public void Save(string key, object? value, string? kind, bool existed)
    {
        if (_loadError != null) throw new IOException("backup.json", _loadError);
        // Keep the first snapshot so retries or a second apply still restore the original value.
        if (_values.ContainsKey(key)) return;
        Commit(new(_values) { [key] = new(value?.ToString(), kind, existed) });
    }

    public void Forget(string key)
    {
        var next = new Dictionary<string, BackupValue>(_values);
        if (next.Remove(key)) Commit(next);
    }

    private void Commit(Dictionary<string, BackupValue> next)
    {
        // Stage beside the destination so replacing the JSON does not cross volumes.
        string tmp = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(next));
        File.Move(tmp, _path, true);
        _values = next;
    }
}

public sealed record BackupValue(string? Value, string? Kind, bool Existed);
