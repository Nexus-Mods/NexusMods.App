using NexusMods.Common;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.StreamFactories;

/// <summary>
/// A Stream Factory that's backed by a <see cref="MemoryStream"/>.
/// </summary>
public class MemoryStreamFactory : IStreamFactory, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly bool _takeOwnership;

    /// <summary>
    /// Creates a new <see cref="MemoryStreamFactory"/> instance. Infers the <see cref="Size"/> from the <paramref name="stream"/>.
    /// Sets the <see cref="LastModifiedUtc"/> to <see cref="DateTime.UtcNow"/> if <paramref name="lastModifiedUtc"/> is null.
    /// Sets the <see cref="Name"/> to <paramref name="name"/>.
    /// If <paramref name="takeOwnership"/> is true, the <paramref name="stream"/> will be disposed when this instance is disposed.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="stream"></param>
    /// <param name="lastModifiedUtc"></param>
    /// <param name="takeOwnership"></param>
    public MemoryStreamFactory(IPath name, MemoryStream stream, DateTime? lastModifiedUtc = null, bool takeOwnership = true)
    {
        _stream = stream;
        _takeOwnership = takeOwnership;
        Name = name;
        Size = Size.FromLong(_stream.Length);
        LastModifiedUtc = lastModifiedUtc ?? DateTime.UtcNow;
    }

    /// <inheritdoc />
    public DateTime LastModifiedUtc { get; }

    /// <inheritdoc />
    public IPath Name { get; }

    /// <inheritdoc />
    public Size Size { get; }

    /// <inheritdoc />
    public ValueTask<Stream> GetStreamAsync()
    {
        return ValueTask.FromResult<Stream>(_stream);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_takeOwnership) 
            _stream.Dispose();
    }
}
