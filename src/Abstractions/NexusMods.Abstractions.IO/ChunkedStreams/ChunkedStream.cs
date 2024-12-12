using System.Buffers;
using System.Diagnostics;
using Reloaded.Memory.Extensions;

namespace NexusMods.Abstractions.IO.ChunkedStreams;

/// <summary>
/// A stream that reads data in chunks, caching the chunks in a cache and allowing
/// for random access to the chunks presented as a stream.
/// </summary>
public class ChunkedStream<T> : Stream where T : IChunkedStreamSource
{
    private ulong _position;
    private readonly T _source;
    private LightweightLRUCache<ulong, IMemoryOwner<byte>> _cache;
    private readonly MemoryPool<byte> _pool;

    /// <summary>
    /// Main constructor, creates a new Chunked stream from the given source, and with a LRU cache of the given size
    /// </summary>
    /// <param name="source"></param>
    /// <param name="capacity"></param>
    public ChunkedStream(T source, int capacity = 16)
    {
        _position = 0;
        _source = source;
        _pool = MemoryPool<byte>.Shared;
        _cache = new LightweightLRUCache<ulong, IMemoryOwner<byte>>(capacity);
    }

    /// <inheritdoc />
    public override void Flush() { }

    /// <inheritdoc />
    public override int Read(byte[] buffer, int offset, int count) => Read(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        if (_position >= _source.Size.Value)
        {
            return 0;
        }

        var chunkIdx = FindChunkIndex(_position);
        var chunkOffset = _position - _source.GetOffset(chunkIdx);
        var chunkSize = _source.GetChunkSize(chunkIdx);
        var chunk = GetChunk(chunkIdx)[..chunkSize];
        var readToEnd = Math.Clamp(_source.Size.Value - _position, 0, Int32.MaxValue);

        var toRead = Math.Min(buffer.Length, chunk.Length - (int)chunkOffset);
        toRead = Math.Min(toRead, (int)readToEnd);

        chunk.Slice((int)chunkOffset, toRead)
            .Span
            .CopyTo(buffer);
        _position += (ulong)toRead;
        
        Debug.Assert(_position <= _source.Size.Value, "Read more than the size of the stream");
        return toRead;
    }

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var memory = new Memory<byte>(buffer, offset, count);
        return ReadAsync(memory, cancellationToken).AsTask();
    }
    
    /// <inheritdoc />
    public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
        CancellationToken cancellationToken = new())
    {
        if (_position >= _source.Size.Value)
        {
            return 0;
        }

        var chunkIdx = FindChunkIndex(_position);
        var chunkOffset = _position - _source.GetOffset(chunkIdx);
        var chunkSize = _source.GetChunkSize(chunkIdx);
        var chunk = (await GetChunkAsync(chunkIdx, cancellationToken))[..chunkSize];
        var readToEnd = Math.Clamp(_source.Size.Value - _position, 0, Int32.MaxValue);

        var toRead = Math.Min(buffer.Length, chunk.Length - (int)chunkOffset);
        toRead = Math.Min(toRead, (int)readToEnd);

        chunk.Slice((int)chunkOffset, toRead)
            .Span
            .CopyTo(buffer.Span);
        _position += (ulong)toRead;
        Debug.Assert(_position <= _source.Size.Value, "Read more than the size of the stream");
        return toRead;
    }

    private async ValueTask<Memory<byte>> GetChunkAsync(ulong index, CancellationToken token)
    {
        if (_cache.TryGet(index, out var memory))
        {
            return memory!.Memory;
        }

        var chunkSize = _source.GetChunkSize(index);
        var memoryOwner = _pool.Rent(chunkSize);
        await _source.ReadChunkAsync(memoryOwner.Memory[..chunkSize], index, token);
        _cache.Add(index, memoryOwner);
        return memoryOwner.Memory;
    }

    private Memory<byte> GetChunk(ulong index)
    {
        if (_cache.TryGet(index, out var memory))
        {
            return memory!.Memory;
        }

        var chunkSize = _source.GetChunkSize(index);
        var memoryOwner = _pool.Rent(chunkSize);
        var chunkMemory = memoryOwner.Memory[..chunkSize];
        _source.ReadChunk(chunkMemory.Span, index);
        _cache.Add(index, memoryOwner);
        return chunkMemory;
    }

    private ulong FindChunkIndex(ulong position)
    {
        ulong low = 0, high = _source.ChunkCount - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var startOffset = _source.GetOffset(mid);
            var nextOffset = mid + 1 < _source.ChunkCount ? _source.GetOffset(mid + 1) : _source.Size.Value;

            if (position >= startOffset && position < nextOffset)
            {
                return mid;
            }

            if (position < startOffset)
            {
                high = mid - 1;
            }
            else
            {
                low = mid + 1;
            }
        }
        throw new InvalidOperationException("Position out of range.");
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Begin:
                _position = (ulong)offset;
                break;
            case SeekOrigin.Current:
                _position += (ulong)offset;
                break;
            case SeekOrigin.End:
                _position = _source.Size.Value + (ulong)offset;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin), origin, null);
        }

        return (long)_position;
    }

    /// <summary>
    /// Not implemented as this is a read-only stream.
    /// </summary>
    /// <param name="value"></param>
    /// <exception cref="NotImplementedException"></exception>
    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    /// <summary>
    /// Not implemented as this is a read-only stream.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <exception cref="NotImplementedException"></exception>
    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    /// <inheritdoc />
    public override bool CanRead => true;

    /// <inheritdoc />
    public override bool CanSeek => true;

    /// <inheritdoc />
    public override bool CanWrite => false;

    /// <inheritdoc />
    public override long Length => (long)_source.Size.Value;

    /// <inheritdoc />
    public override long Position
    {
        get => (long)_position;
        set => Seek(value, SeekOrigin.Begin);
    }
}
