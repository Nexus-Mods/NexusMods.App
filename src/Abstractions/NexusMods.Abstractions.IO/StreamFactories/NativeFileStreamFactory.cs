using NexusMods.Paths;

namespace NexusMods.Abstractions.IO.StreamFactories;

/// <summary>
/// Represents a Stream Factory that's backed by a native path on the FileSystem.
/// </summary>
public class NativeFileStreamFactory : IStreamFactory
{
    private AbsolutePath _file;
    private DateTime? _lastModifiedCache;

    /// <inheritdoc />
    public Size Size => _file.FileInfo.Size;

    /// <inheritdoc />
    public IPath Name => _file;

    /// <summary>
    /// Absolute path to the file.
    /// </summary>
    public AbsolutePath Path => _file;

    /// <summary/>
    /// <param name="file">Absolute path of the file.</param>
    public NativeFileStreamFactory(AbsolutePath file) => _file = file;

    /// <inheritdoc />
    public ValueTask<Stream> GetStreamAsync()
    {
        return new ValueTask<Stream>(_file.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}
