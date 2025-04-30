using NexusMods.Paths;

namespace NexusMods.Abstractions.IO.ChunkedStreams;

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
    /// The number of chunks in the source.
    /// </summary>
    public ulong ChunkCount { get; }

    /// <summary>
    /// Gets the starting offset (relative to the start of the file) of the given chunk index.
    /// </summary>
    public ulong GetOffset(ulong chunkIndex);

    /// <summary>
    /// Gets the size of a chunk given its index.
    /// </summary>
    public int GetChunkSize(ulong chunkIndex);

    /// <summary>
    /// Reads a chunk from the source into the buffer.
    /// </summary>
    public Task ReadChunkAsync(Memory<byte> buffer, ulong chunkIndex, CancellationToken token = default);

    /// <summary>
    /// Reads a chunk from the source into the buffer.
    /// </summary>
    public void ReadChunk(Span<byte> buffer, ulong chunkIndex);
}
