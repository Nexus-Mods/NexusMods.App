using NexusMods.Common;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.StreamFactories;

/// <summary>
/// Represents a Stream Factory that's backed by a native path on the FileSystem.
/// </summary>
public class NativeFileStreamFactory : IStreamFactory
{
    private AbsolutePath _file;
    private DateTime? _lastModifiedCache;

    /// <inheritdoc />
    public Size Size => _file.Length;

    /// <inheritdoc />
    public IPath Name => _file;

    /// <summary/>
    /// <param name="file">Absolute path of the file.</param>
    public NativeFileStreamFactory(AbsolutePath file) => _file = file;

    /// <inheritdoc />
    public ValueTask<Stream> GetStream()
    {
        return new ValueTask<Stream>(_file.Open(FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    /// <inheritdoc />
    public DateTime LastModifiedUtc
    {
        get
        {
            _lastModifiedCache ??= _file.LastWriteTimeUtc;
            return _lastModifiedCache.Value;
        }
    }
}