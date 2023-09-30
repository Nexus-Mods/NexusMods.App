using NexusMods.Paths;

namespace NexusMods.DataModel.ChunkedStreams;

/// <summary>
/// A source of data for a <see cref="ChunkedStream{T}"/>. Sizes of chunks should be no larger
/// than 1MB or the chunked stream should be updated to not use a memory pool in those cases.
/// </summary>
public interface IChunkedStreamSource
{
    /// <summary>
    /// Gets the size of the source in bytes.
    /// </summary>
    public Size Size { get; }

    /// <summary>
    /// The size of the chunks. Every chunk must be the same size, except for the last chunk which can be
    /// smaller. The final chunk must not be empty or larger than the chunk size. All chunks (except the last)
    /// must be no smaller than 16KB.
    /// </summary>
    public Size ChunkSize { get; }

    /// <summary>
    /// The number of chunks in the source.
    /// </summary>
    public ulong ChunkCount { get; }

    /// <summary>
    /// Reads a chunk from the source into the buffer. The buffer size will always be the same as `ChunkSize`
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="chunkIndex"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default);

    /// <summary>
    /// Reads a chunk from the source into the buffer. The buffer size will always be the same as `ChunkSize`
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="chunkIndex"></param>
    public void ReadChunk(Span<byte> buffer, ulong chunkIndex);
}
