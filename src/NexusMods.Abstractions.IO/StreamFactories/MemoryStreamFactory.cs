using NexusMods.Paths;

namespace NexusMods.Abstractions.IO.StreamFactories;

/// <summary>
/// A Stream Factory that's backed by a <see cref="MemoryStream"/>.
/// </summary>
public class MemoryStreamFactory : IStreamFactory, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly bool _takeOwnership;

    /// <summary>
    /// Creates a new <see cref="MemoryStreamFactory"/> instance. Infers the <see cref="Size"/> from the <paramref name="stream"/>.
    /// Sets the <see cref="Name"/> to <paramref name="name"/>.
    /// If <paramref name="takeOwnership"/> is true, the <paramref name="stream"/> will be disposed when this instance is disposed.
    /// </summary>
    public MemoryStreamFactory(IPath name, MemoryStream stream, bool takeOwnership = true)
    {
        _stream = stream;
        _takeOwnership = takeOwnership;
        Name = name;
        Size = Size.FromLong(_stream.Length);
    }

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
