using NexusMods.Common;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.StreamFactories;

/// <summary>
/// A Stream Factory that's backed by a <see cref="MemoryStream"/>.
/// </summary>
public class MemoryStreamFactory : IStreamFactory
{
    private readonly MemoryStream _stream;

    /// <summary>
    /// Creates a new <see cref="MemoryStreamFactory"/> instance. Infers the <see cref="Size"/> from the <paramref name="stream"/>.
    /// Sets the <see cref="LastModifiedUtc"/> to <see cref="DateTime.UtcNow"/> if <paramref name="lastModifiedUtc"/> is null.
    /// Sets the <see cref="Name"/> to <paramref name="name"/>.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="stream"></param>
    /// <param name="lastModifiedUtc"></param>
    public MemoryStreamFactory(IPath name, MemoryStream stream, DateTime? lastModifiedUtc = null)
    {
        _stream = stream;
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
}
