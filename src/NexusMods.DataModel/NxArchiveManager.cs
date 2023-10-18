using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ChunkedStreams;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

/// <summary>
/// Manages the archive locations and allows for the backup of files to internal data folders.
/// </summary>
public class NxArchiveManager : IArchiveManager
{
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IDataStore _store;
    private readonly ILogger<NxArchiveManager> _logger;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="store"></param>
    /// <param name="settings"></param>
    public NxArchiveManager(ILogger<NxArchiveManager> logger, IDataStore store, IDataModelSettings settings)
    {
        _archiveLocations = settings.ArchiveLocations.Select(f => f.ToAbsolutePath()).ToArray();
        foreach (var location in _archiveLocations)
        {
            if (!location.DirectoryExists())
                location.CreateDirectory();
        }

        _logger = logger;
        _store = store;

    }

    /// <inheritdoc />
    public ValueTask<bool> HaveFile(Hash hash)
    {
        return ValueTask.FromResult(TryGetLocation(hash, out _, out _));
    }

    /// <inheritdoc />
    public async Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, CancellationToken token = default)
    {
        var builder = new NxPackerBuilder();
        var distinct = backups.DistinctBy(d => d.Hash).ToArray();
        var streams = new List<Stream>();
        _logger.LogDebug("Backing up {Count} files of {Size} in size", distinct.Length, distinct.Sum(s => s.Size));
        foreach (var backup in distinct)
        {
            var stream = await backup.StreamFactory.GetStreamAsync();
            streams.Add(stream);
            builder.AddFile(stream, new AddFileParams
            {
                RelativePath = backup.Hash.ToHex(),
            });
        }

        var guid = Guid.NewGuid();
        var id = guid.ToString();
        var outputPath = _archiveLocations.First().Combine(id).AppendExtension(KnownExtensions.Tmp);

        await using (var outputStream = outputPath.Create()){
            builder.WithOutput(outputStream);
            builder.Build();
        }

        foreach (var stream in streams)
            await stream.DisposeAsync();

        var finalPath = outputPath.ReplaceExtension(KnownExtensions.Nx);

        await outputPath.MoveToAsync(finalPath, token: token);
        await using var os = finalPath.Read();
        var unpacker = new NxUnpacker(new FromStreamProvider(os));
        UpdateIndexes(unpacker, distinct, guid, finalPath);
    }

    private unsafe void UpdateIndexes(NxUnpacker unpacker, ArchivedFileEntry[] distinct, Guid guid,
        AbsolutePath finalPath)
    {
        Span<byte> buffer = stackalloc byte[sizeof(NativeFileEntryV1)];

        foreach (var entry in unpacker.GetPathedFileEntries())
        {
            fixed (byte* ptr = buffer)
            {
                var writer = new LittleEndianWriter(ptr);
                entry.Entry.WriteAsV1(ref writer);

                var hash = Hash.FromHex(entry.FileName);
                var dbId = IdFor(hash, guid);
                var dbEntry = new ArchivedFiles
                {
                    File = finalPath.FileName,
                    FileEntryData = buffer.ToArray()
                };

                // TODO: Consider a bulk-put operation here
                _store.Put(dbId, dbEntry);
            }
        }
    }

    private IId IdFor(Hash hash, Guid guid)
    {
        Span<byte> buffer = stackalloc byte[24];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, hash.Value);
        guid.TryWriteBytes(buffer.SliceFast(8));
        return IId.FromSpan(EntityCategory.ArchivedFiles, buffer);
    }

    /// <inheritdoc />
    public async Task ExtractFiles(IEnumerable<(Hash Src, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        var grouped = files.Distinct()
            .Select(input => TryGetLocation(input.Src, out var archivePath, out var fileEntry)
                ? (true, Hash:input.Src, ArchivePath:archivePath, FileEntry:fileEntry, input.Dest)
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => (l.Hash, l.FileEntry, l.Dest));

        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Hash.ToHex()}");

        var settings = new UnpackerSettings();

        foreach (var group in grouped)
        {
            await using var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var toExtract = group
                .Select(entry =>
                    (IOutputDataProvider)new OutputFileProvider(entry.Dest.Parent.GetFullPath(), entry.Dest.FileName, entry.FileEntry))
                .ToArray();

            try
            {
                unpacker.ExtractFiles(toExtract, settings);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            foreach (var toDispose in toExtract)
            {
                toDispose.Dispose();
            }
        }
    }

    /// <inheritdoc />
    public Task<IDictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        var results = new Dictionary<Hash, byte[]>();

        var grouped = files.Distinct()
            .Select(hash => TryGetLocation(hash, out var archivePath, out var fileEntry)
                ? (true, Hash:hash, ArchivePath:archivePath, FileEntry:fileEntry)
                : default)
            .Where(x => x.Item1)
            .ToLookup(l => l.ArchivePath, l => (l.Hash, l.FileEntry));

        if (grouped[default].Any())
            throw new Exception($"Missing archive for {grouped[default].First().Hash.ToHex()}");

        var settings = new UnpackerSettings
        {
            MaxNumThreads = 1
        };
        foreach (var group in grouped)
        {
            var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var infos = group.Select(entry => (entry.Hash, new OutputArrayProvider("", entry.FileEntry), entry.FileEntry.DecompressedSize)).ToList();

            unpacker.ExtractFiles(infos.Select(o => (IOutputDataProvider)o.Item2).ToArray(), settings);
            foreach (var (hash, output, size) in infos)
            {
                results.Add(hash, output.Data[..(int)size]);
            }
        }

        return Task.FromResult<IDictionary<Hash, byte[]>>(results);
    }

    /// <inheritdoc />
    public async Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
    {
        if (hash == Hash.Zero)
            throw new ArgumentNullException(nameof(hash));
        if (!TryGetLocation(hash, out var archivePath, out var entry))
            throw new Exception($"Missing archive for {hash.ToHex()}");

        var file = archivePath.Read();

        var provider = new FromStreamProvider(file);
        var header = HeaderParser.ParseHeader(provider);

        return new ChunkedStream<ChunkedArchiveStream>(new ChunkedArchiveStream(entry, header, file));
    }

    private class ChunkedArchiveStream : IChunkedStreamSource
    {
        private FileEntry _entry;
        private readonly ParsedHeader _header;
        private readonly List<ExtractableBlock> _blocks;
        private readonly Stream _stream;


        public ChunkedArchiveStream(FileEntry entry, ParsedHeader header, Stream stream)
        {
            _entry = entry;
            _header = header;
            _stream = stream;
            _blocks = new List<ExtractableBlock>();
            MakeBlocks(_header.Header.ChunkSizeBytes);
        }

        public Size Size => Size.From(_entry.DecompressedSize);
        public Size ChunkSize => Size.From((ulong)_header.Header.ChunkSizeBytes);
        public ulong ChunkCount => (ulong)_entry.GetChunkCount(_header.Header.ChunkSizeBytes);

        public async Task ReadChunkAsync(Memory<byte> buffer, ulong localIndex, CancellationToken token = default)
        {
            var extractable = PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize, out var offset);
            _stream.Position = offset;
            using var compressedBlock = MemoryPool<byte>.Shared.Rent(compressedBlockSize);
            await _stream.ReadExactlyAsync(compressedBlock.Memory[..compressedBlockSize], token);
            ProcessBlock(buffer.Span, blockIndex, extractable, compressedBlock.Memory.Span, compressedBlockSize);
        }

        public void ReadChunk(Span<byte> buffer, ulong localIndex)
        {
            var extractable = PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize, out var offset);
            _stream.Position = offset;
            using var compressedBlock = MemoryPool<byte>.Shared.Rent(compressedBlockSize);
            _stream.ReadExactly(compressedBlock.Memory.Span[..compressedBlockSize]);
            ProcessBlock(buffer, blockIndex, extractable, compressedBlock.Memory.Span, compressedBlockSize);
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
        private ExtractableBlock PreProcessBlock(ulong localIndex, out int blockIndex, out int compressedBlockSize, out long offset)
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
        private unsafe void ProcessBlock(Span<byte> buffer, int blockIndex, ExtractableBlock extractable, Span<byte> compressedBlock,
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
                    Compression.Decompress(method, compressedPtr, blockSize, ptr, (int)length);
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
                Compression.Decompress(method, compressedPtr, blockSize, extractedPtr,
                    extractable.DecompressSize);


                // Get block index.
                var blockIndexOffset = extractable.BlockIndex - _entry.FirstBlockIndex;
                var start = (long)chunkSize * blockIndexOffset;
                var decompSizeInChunk = _entry.DecompressedSize - (ulong)start;
                var length = Math.Min((long)decompSizeInChunk, chunkSize);

                fixed (byte* ptr = buffer)
                {
                    Buffer.MemoryCopy(extractedPtr + _entry.DecompressedBlockOffset, ptr, length, length);
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
                var block = new ExtractableBlock
                {
                    BlockIndex = blockIndex,
                    DecompressSize = _entry.DecompressedBlockOffset + (int)Math.Min(remainingDecompSize, (ulong)chunkSize)
                };

                _blocks.Add(block);

                remainingDecompSize -= (ulong)chunkSize;
            }
        }
    }

    internal struct ExtractableBlock
    {
        /// <summary>
        ///     Index of block to decompress.
        /// </summary>
        public required int BlockIndex { get; init; }

        /// <summary>
        ///     Amount of data to decompress in this block.
        ///     This is equivalent to largest <see cref="FileEntry.DecompressedBlockOffset" /> +
        ///     <see cref="FileEntry.DecompressedSize" /> for a file within the block.
        /// </summary>
        public required int DecompressSize { get; set; }
    }

    private unsafe bool TryGetLocation(Hash hash, out AbsolutePath archivePath, out FileEntry fileEntry)
    {
        var prefix = new Id64(EntityCategory.ArchivedFiles, (ulong)hash);
        foreach (var entry in _store.GetByPrefix<ArchivedFiles>(prefix))
        {
            foreach (var location in _archiveLocations)
            {
                var path = location.Combine(entry.File);
                if (!path.FileExists) continue;

                archivePath = path;

                fixed (byte* ptr = entry.FileEntryData.AsSpan())
                {
                    var reader = new LittleEndianReader(ptr);
                    FileEntry tmpEntry = default;

                    tmpEntry.FromReaderV1(ref reader);
                    fileEntry = tmpEntry;
                    return true;
                }
            }

        }

        archivePath = default;
        fileEntry = default;
        return false;
    }
}
