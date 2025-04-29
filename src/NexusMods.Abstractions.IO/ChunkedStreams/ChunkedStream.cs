using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;

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
    private readonly Dictionary<ulong, Task<IMemoryOwner<byte>>> _preFetchingTasks = new();
    private readonly int _preFetch;

    /// <summary>
    /// Main constructor, creates a new Chunked stream from the given source, and with an LRU cache of the given size. If preFetch is greater than 0,
    /// the stream will attempt to read ahead of any read requests by the given amount of chunks, this can be useful when the chunk source is slow or bursty,
    /// and chunks are small.
    /// </summary>
    public ChunkedStream(T source, int capacity = 16, int preFetch = 0)
    {
        _position = 0;
        _source = source;
        _pool = MemoryPool<byte>.Shared;
        _cache = new LightweightLRUCache<ulong, IMemoryOwner<byte>>(capacity);
        _preFetch = preFetch;
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
        var readToEnd = Math.Clamp(_source.Size.Value - _position, 0, int.MaxValue);

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
        // Prefetch code
        // If _preLoad is 0 here, then this block is skipped and we never pre-fetch anything
        for (var i = index + 1; i < index + (ulong)_preFetch ; i++)
        {
            if (i >= _source.ChunkCount)
                break;
            
            // If we already have this chunk cached, move on
            if (_cache.TryGet(i, out _))
                continue;

            // If we're already preloading this chunk, move on
            if (_preFetchingTasks.ContainsKey(i))
                continue;

            // Create the chunk task. It begins executing here
            _preFetchingTasks[i] = AllocateAndGetChunk(i, token);
        }
        
        // If the chunk is cached, use it
        if (_cache.TryGet(index, out var memory))
        {
            return memory!.Memory;
        }
        
        // If the chunk is being preloaded (could have been in a previous run of this method) use that
        if (_preFetchingTasks.TryGetValue(index, out var task))
        {
            var chunk = await task;
            _cache.Add(index, chunk);
            _preFetchingTasks.Remove(index);
            return chunk.Memory;
        }

        // Otherwise load the chunk
        var memoryOwner = await AllocateAndGetChunk(index, token);
        _cache.Add(index, memoryOwner);
        return memoryOwner.Memory;
    }

    private async Task<IMemoryOwner<byte>> AllocateAndGetChunk(ulong index, CancellationToken token)
    {
        var chunkSize = _source.GetChunkSize(index);
        var memoryOwner = _pool.Rent(chunkSize);
        await _source.ReadChunkAsync(memoryOwner.Memory[..chunkSize], index, token);
        return memoryOwner;
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

    /// <summary>
    /// Performs a binary search of the chunks to find the chunk index that contains the given position.
    /// </summary>
    private ulong FindChunkIndex(ulong position)
    {
        ulong low = 0, high = _source.ChunkCount - 1;
        while (low <= high)
        {
            var mid = (low + high) / 2;
            var startOffset = _source.GetOffset(mid);
            
            ulong nextOffset;
            if (mid + 1 < _source.ChunkCount)
                nextOffset = _source.GetOffset(mid + 1);
            else
                nextOffset = _source.Size.Value;

            if (position >= startOffset && position < nextOffset)
                return mid;

            if (position < startOffset)
                high = mid - 1;
            else
                low = mid + 1;
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
    
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _cache.Dispose();
        }
    }
}
