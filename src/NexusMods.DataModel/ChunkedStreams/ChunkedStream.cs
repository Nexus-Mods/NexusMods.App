using System.Buffers;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.ChunkedStreams;

/// <summary>
/// A stream that reads data in chunks, caching the chunks in a cache and allowing
/// for random access to the chunks presented as a stream.
/// </summary>
public class ChunkedStream : Stream
{
    private ulong _position;
    private readonly IChunkedStreamSource _source;
    private LightweightLRUCache<ulong, IMemoryOwner<byte>> _cache;
    private readonly MemoryPool<byte> _pool;

    /// <summary>
    /// Main constructor, creates a new Chunked stream from the given source, and with a LRU cache of the given size
    /// </summary>
    /// <param name="source"></param>
    /// <param name="capacity"></param>
    public ChunkedStream(IChunkedStreamSource source, int capacity = 16)
    {
        _position = 0;
        _source = source;
        _pool = MemoryPool<byte>.Shared;
        _cache = new LightweightLRUCache<ulong, IMemoryOwner<byte>>(capacity);
    }

    /// <inheritdoc />
    public override void Flush()
    {
    }


    /// <summary>
    /// ChunkedStream is async only, so this will throw.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="offset"></param>
    /// <param name="count"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_position >= _source.Size.Value)
        {
            return 0;
        }

        var chunkIdx = _position / _source.ChunkSize.Value;
        var chunkOffset = _position % _source.ChunkSize.Value;
        var isLastChunk = chunkIdx == _source.ChunkCount - 1;
        var chunk = GetChunk(chunkIdx);

        var toRead = Math.Min(count, (int)(_source.ChunkSize.Value - chunkOffset));
        if (isLastChunk)
        {
            var lastChunkExtraSize = _source.Size.Value % _source.ChunkSize.Value;
            if (lastChunkExtraSize > 0)
            {
                toRead = Math.Min(toRead, (int)lastChunkExtraSize);
            }
        }
        chunk.Slice((int)chunkOffset, toRead)
            .Span
            .CopyTo(buffer.AsSpan(offset, toRead));
        _position += (ulong)toRead;
        return toRead;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
    {
        if (_position >= _source.Size.Value)
        {
            return 0;
        }

        var chunkIdx = _position / _source.ChunkSize.Value;
        var chunkOffset = _position % _source.ChunkSize.Value;
        var isLastChunk = chunkIdx == _source.ChunkCount - 1;
        var chunk = await GetChunkAsync(chunkIdx, cancellationToken);

        var toRead = Math.Min(buffer.Length, (int)(_source.ChunkSize.Value - chunkOffset));
        if (isLastChunk)
        {
            var lastChunkExtraSize = _source.Size.Value % _source.ChunkSize.Value;
            if (lastChunkExtraSize > 0)
            {
                toRead = Math.Min(toRead, (int)lastChunkExtraSize);
            }
        }
        chunk.Slice((int)chunkOffset, toRead)
            .Span
            .CopyTo(buffer.Span.SliceFast(0, toRead));
        _position += (ulong)toRead;
        return toRead;

    }

    private async ValueTask<Memory<byte>> GetChunkAsync(ulong index, CancellationToken token)
    {
        if (_cache.TryGet(index, out var memory))
        {
            return memory!.Memory;
        }
        var memoryOwner = _pool.Rent((int)_source.ChunkSize.Value);
        await _source.ReadChunkAsync(memoryOwner.Memory, index, token);
        _cache.Add(index, memoryOwner);
        return memoryOwner.Memory;
    }

    private Memory<byte> GetChunk(ulong index)
    {
        if (_cache.TryGet(index, out var memory))
        {
            return memory!.Memory;
        }
        var memoryOwner = _pool.Rent((int)_source.ChunkSize.Value);
        _source.ReadChunk(memoryOwner.Memory.Span, index);
        _cache.Add(index, memoryOwner);
        return memoryOwner.Memory;
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
        throw new NotImplementedException();
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
        throw new NotImplementedException();
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
