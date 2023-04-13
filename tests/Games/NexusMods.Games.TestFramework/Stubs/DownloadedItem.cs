using NexusMods.Paths;

namespace NexusMods.Games.TestFramework.Stubs;

/// <summary>
/// Represents an item downloaded for purpose of unit testing.
/// </summary>
public class DownloadedItem : IDisposable
{
    /// <summary>
    /// Path to downloaded item in target filesystem.
    /// </summary>
    public TemporaryPath Path { get; }

    /// <summary>
    /// The target filesystem in question.
    /// </summary>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Manifest associated with this item.
    /// </summary>
    public RemoteModMetadataBase Manifest { get; }

    private readonly TemporaryFileManager _manager;
    private bool _disposed;

    public DownloadedItem(TemporaryFileManager manager, TemporaryPath path,
        IFileSystem fileSystem, RemoteModMetadataBase manifest)
    {
        _manager = manager;
        Path = path;
        FileSystem = fileSystem;
        Manifest = manifest;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _manager.Dispose();
        Path.Dispose();
        GC.SuppressFinalize(this);
    }
}
