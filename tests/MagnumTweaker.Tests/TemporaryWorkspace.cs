namespace MagnumTweaker.Tests;

internal sealed class TemporaryWorkspace : IDisposable
{
    public string Root { get; } = Directory.CreateTempSubdirectory("MagnumTweaker.Tests.").FullName;
    public BackupStore Backups => new(Root);
    public string FilePath(string relativePath) => Path.Combine(Root, relativePath);

    public void Dispose() => Directory.Delete(Root, recursive: true);
}
