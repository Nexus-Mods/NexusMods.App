using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
/// A Stream Factory that's backed by a <see cref="MemoryStream"/>.
/// </summary>
public sealed class MemoryStreamFactory : IStreamFactory, IDisposable
{
    private readonly MemoryStream _stream;
    private readonly bool _takeOwnership;

    /// <summary>
    /// Constructor.
    /// </summary>
    public MemoryStreamFactory(RelativePath name, MemoryStream stream, bool takeOwnership = true)
    {
        _stream = stream;
        _takeOwnership = takeOwnership;
        FileName = name;
    }

    /// <inheritdoc />
    public RelativePath FileName { get; }

    /// <inheritdoc />
    public ValueTask<Stream> GetStreamAsync()
    {
        return ValueTask.FromResult<Stream>(_stream);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_takeOwnership) _stream.Dispose();
    }
}
