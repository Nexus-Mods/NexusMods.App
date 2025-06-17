using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Sdk.IO;

/// <summary>
/// Sub-stream on existing stream.
/// </summary>
[PublicAPI]
public class SubStream : Stream
{
    private readonly Stream _parent;
    private readonly long _offset;
    private readonly long _length;

    /// <summary>
    /// Constructor.
    /// </summary>
    public SubStream(Stream parent, Size offset, Size length)
    {
        _parent = parent;
        _offset = (long)offset.Value;
        _length = (long)length.Value;
    }

    /// <inheritdoc/>
    public override void Flush() { }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (count > _length - Position)
        {
            count = (int)(_length - Position);
        }
        
        _parent.Position = _offset + Position;
        var read = _parent.Read(buffer, offset, count);
        Position += read;
        return read;
    }

    /// <inheritdoc/>
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        var count = buffer.Length;
        if (count > _length - Position)
        {
            count = (int)(_length - Position);
        }

        if (count == 0) return 0;

        _parent.Position = _offset + Position;
        var read = await _parent.ReadAsync(buffer[..count], cancellationToken);
        Position += read;
        return read;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin)
    {
        return origin switch
        {
            SeekOrigin.Begin => Position = offset,
            SeekOrigin.Current => Position += offset,
            SeekOrigin.End => Position = _length + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin), origin, null),
        };
    }


    /// <inheritdoc />
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => _length;

    /// <inheritdoc />
    public override long Position { get; set; }
}
