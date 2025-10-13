using System.Buffers;
using System.Runtime.CompilerServices;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class ChunkedArchiveStream : IChunkedStreamSource
{
    private FileEntry _entry;
    private readonly ParsedHeader _header;
    private readonly List<NxFileStore.ExtractableBlock> _blocks;
    private readonly Stream _stream;

    public ChunkedArchiveStream(FileEntry entry, ParsedHeader header, Stream stream)
    {
        _entry = entry;
        _header = header;
        _stream = stream;
        _blocks = new List<NxFileStore.ExtractableBlock>();
        MakeBlocks(_header.Header.ChunkSizeBytes);
    }

    public Size Size => Size.From(_entry.DecompressedSize);
    public Size ChunkSize => Size.From((ulong)_header.Header.ChunkSizeBytes);
    public ulong ChunkCount => (ulong)_entry.GetChunkCount(_header.Header.ChunkSizeBytes);

    public ulong GetOffset(ulong chunkIndex)
    {
        return (ulong)_header.Header.ChunkSizeBytes * chunkIndex;
    }

    public int GetChunkSize(ulong chunkIndex)
    {
        return _header.Header.ChunkSizeBytes;
    }

    public async Task ReadChunkAsync(Memory<byte> buffer, ulong localIndex, CancellationToken token = default)
    {
        var extractable =
            PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize,
                out var offset);
        _stream.Position = (long)offset;
        using var compressedBlock = MemoryPool<byte>.Shared.Rent(compressedBlockSize);
        await _stream.ReadExactlyAsync(compressedBlock.Memory[..compressedBlockSize], token);
        ProcessBlock(buffer.Span, blockIndex, extractable,
            compressedBlock.Memory.Span, compressedBlockSize);
    }

    public void ReadChunk(Span<byte> buffer, ulong localIndex)
    {
        var extractable =
            PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize,
                out var offset);
        _stream.Position = (long)offset;
        using var compressedBlock = MemoryPool<byte>.Shared.Rent(compressedBlockSize);
        _stream.ReadExactly(compressedBlock.Memory.Span[..compressedBlockSize]);
        ProcessBlock(buffer, blockIndex, extractable,
            compressedBlock.Memory.Span, compressedBlockSize);
    }

    /// <summary>
    /// Performs all the pre-processing logic for a block, which means calculating offsets and the like
    /// </summary>
    /// <param name="localIndex"></param>
    /// <param name="blockIndex"></param>
    /// <param name="compressedBlockSize"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private NxFileStore.ExtractableBlock PreProcessBlock(ulong localIndex, out int blockIndex, out int compressedBlockSize,
        out ulong offset)
    {
        var extractable = _blocks[(int)localIndex];
        blockIndex = extractable.BlockIndex;
        compressedBlockSize = _header.Blocks[blockIndex].CompressedSize;
        offset = _header.BlockOffsets[blockIndex];
        return extractable;
    }

    /// <summary>
    /// All the post-processing logic for a block, including decompression, this is put in a function so it
    /// can be called from both sync and async methods.
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="blockIndex"></param>
    /// <param name="extractable"></param>
    /// <param name="compressedBlock"></param>
    /// <param name="blockSize"></param>
    private unsafe void ProcessBlock(Span<byte> buffer, int blockIndex, NxFileStore.ExtractableBlock extractable,
        Span<byte> compressedBlock,
        int blockSize)
    {
        var chunkSize = _header.Header.ChunkSizeBytes;
        var method = _header.BlockCompressions[blockIndex];


        var canFastDecompress = true;
    fallback:
        if (canFastDecompress)
        {
            // This is a hot path in case of 1 output which starts at offset 0.
            // This is common in the case of chunked files extracted to disk.

            if (_entry.DecompressedBlockOffset != 0)
            {
                // This mode is only supported if start of decompressed data is at offset 0 of decompressed buffer.
                // If this is unsupported (rarely in this hot path) we go back to 'slow' approach.
                canFastDecompress = false;
                goto fallback;
            }

            // Get block index.
            var blockIndexOffset = extractable.BlockIndex - _entry.FirstBlockIndex;
            var start = (long)chunkSize * blockIndexOffset;
            var decompSizeInChunk = _entry.DecompressedSize - (ulong)start;
            var length = Math.Min((long)decompSizeInChunk, chunkSize);

            fixed (byte* compressedPtr = compressedBlock)
            fixed (byte* ptr = buffer)
            {
                Compression.Decompress(method, compressedPtr, blockSize,
                    ptr, (int)length);
            }

            return;
        }

        // This is the logic in case of multiple outputs, e.g. if user specifies an Array + File output.
        // It incurs additional memory copies, which may bottleneck when extraction is done purely in RAM.
        // Decompress the needed bytes.
        using var extractedBlock = MemoryPool<byte>.Shared.Rent(extractable.DecompressSize);

        fixed (byte* compressedPtr = compressedBlock)
        fixed (byte* extractedPtr = extractedBlock.Memory.Span)
        {
            // Decompress all.
            Compression.Decompress(method, compressedPtr, blockSize,
                extractedPtr,
                extractable.DecompressSize);


            // Get block index.
            var blockIndexOffset = extractable.BlockIndex - _entry.FirstBlockIndex;
            var start = (long)chunkSize * blockIndexOffset;
            var decompSizeInChunk = _entry.DecompressedSize - (ulong)start;
            var length = Math.Min((long)decompSizeInChunk, chunkSize);

            fixed (byte* ptr = buffer)
            {
                Buffer.MemoryCopy(extractedPtr + _entry.DecompressedBlockOffset, ptr, length,
                    length);
            }
        }
    }

    private void MakeBlocks(int chunkSize)
    {
        // Slow due to copy to stack, but not that big a deal here.
        var chunkCount = _entry.GetChunkCount(chunkSize);
        var remainingDecompSize = _entry.DecompressedSize;

        for (var chunkIndex = 0; chunkIndex < chunkCount; chunkIndex++)
        {
            var blockIndex = _entry.FirstBlockIndex + chunkIndex;
            var block = new NxFileStore.ExtractableBlock
            {
                BlockIndex = blockIndex,
                DecompressSize = _entry.DecompressedBlockOffset +
                                 (int)Math.Min(remainingDecompSize, (ulong)chunkSize),
            };

            _blocks.Add(block);

            remainingDecompSize -= (ulong)chunkSize;
        }
    }
}
