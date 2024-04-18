using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.DataModel;
using NexusMods.Abstractions.Serialization.DataModel.Ids;
using NexusMods.Abstractions.Settings;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Headers.Native;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ChunkedStreams;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
#if DEBUG
using System.Diagnostics;
#endif

namespace NexusMods.DataModel;

/// <summary>
/// A IFileStore implementation that uses the Nx format for storage.
/// </summary>
public class NxFileStore : IFileStore
{
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IDataStore _store;
    private readonly ILogger<NxFileStore> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public NxFileStore(
        ILogger<NxFileStore> logger,
        IDataStore store,
        ISettingsManager settingsManager,
        IFileSystem fileSystem)
    {
        var settings = settingsManager.Get<DataModelSettings>();

        _archiveLocations = settings.ArchiveLocations.Select(f => f.ToPath(fileSystem)).ToArray();
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
        return ValueTask.FromResult(TryGetLocation(hash, null, out _, out _));
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

        await using (var outputStream = outputPath.Create())
        {
            builder.WithOutput(outputStream);
            builder.Build();
        }

        foreach (var stream in streams)
            await stream.DisposeAsync();

        var finalPath = outputPath.ReplaceExtension(KnownExtensions.Nx);

        await outputPath.MoveToAsync(finalPath, token: token);
        await using var os = finalPath.Read();
        var unpacker = new NxUnpacker(new FromStreamProvider(os));
        UpdateIndexes(unpacker, finalPath);
    }

    private unsafe void UpdateIndexes(NxUnpacker unpacker, AbsolutePath finalPath)
    {
        var entries = unpacker.GetPathedFileEntries();
        var items = GC.AllocateUninitializedArray<(IId, ArchivedFile)>(entries.Length);
        Span<byte> buffer = stackalloc byte[sizeof(NativeFileEntryV1)];

        for (var x = 0; x < entries.Length; x++)
        {
            var entry = entries[x];
            fixed (byte* ptr = buffer)
            {
                var writer = new LittleEndianWriter(ptr);
                entry.Entry.WriteAsV1(ref writer);

                var hash = Hash.From(entry.Entry.Hash);
                var dbId = IdFor(hash);
                var dbEntry = new ArchivedFile
                {
                    File = finalPath.FileName,
                    FileEntryData = buffer.ToArray(),
                };

                // TODO: Consider a bulk-put operation here
                items[x] = (dbId, dbEntry);
            }
        }

        _store.PutAll(items.AsSpan());
    }

    [SkipLocalsInit]
    private IId IdFor(Hash hash)
    {
        Span<byte> buffer = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(buffer, hash.Value);
        return IId.FromSpan(EntityCategory.ArchivedFiles, buffer);
    }

    /// <inheritdoc />
    public async Task ExtractFiles((Hash Hash, AbsolutePath Dest)[] files, CancellationToken token = default)
    {
        // Group the files by archive.
        // In almost all cases, everything will go in one archive, except for cases
        // of duplicate files between different mods.
        var groupedFiles = new ConcurrentDictionary<AbsolutePath, List<(Hash Hash, FileEntry FileEntry, AbsolutePath Dest)>>(Environment.ProcessorCount, 1);
        var createdDirectories = new ConcurrentDictionary<AbsolutePath, byte>();
    
#if DEBUG
        var destPaths = new ConcurrentDictionary<AbsolutePath, byte>(); // Sanity test. Test code had this issue.
#endif

        // Capacity is set to 'expected archive count' + 1.
        var fileExistsCache = new ConcurrentDictionary<AbsolutePath, bool>(Environment.ProcessorCount, 2);
        Parallel.ForEach(files, file =>
        {
            if (TryGetLocation(file.Hash, fileExistsCache, out var archivePath, out var fileEntry))
            {
                var group = groupedFiles.GetOrAdd(archivePath, _ => new List<(Hash, FileEntry, AbsolutePath)>());
                lock (group)
                {
                    group.Add((file.Hash, fileEntry, file.Dest));
                }

                // Create the directory, this will speed up extraction in Nx
                // down the road. Usually the difference is negligible, but in
                // extra special with 100s of directories scenarios, it can
                // save a second or two.
                var containingDir = file.Dest.Parent;
                if (createdDirectories.TryAdd(containingDir, 0))
                    containingDir.CreateDirectory();
#if DEBUG
                Debug.Assert(destPaths.TryAdd(file.Dest, 0), $"Duplicate destination path: {file.Dest}. Should not happen.");
#endif
            }
            else
            {
                throw new FileNotFoundException($"Missing archive for {file.Hash.ToHex()}");
            }
        });

        // Extract from all source archives.
        foreach (var group in groupedFiles)
        {
            await using var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            // Make all output providers.
            var toExtract = GC.AllocateUninitializedArray<IOutputDataProvider>(group.Value.Count);
            Parallel.For(0, group.Value.Count, x =>
            {
                var entry = group.Value[x];
                toExtract[x] = new OutputFileProvider(entry.Dest.Parent.GetFullPath(), entry.Dest.FileName, entry.FileEntry);
            });

            try
            {
                unpacker.ExtractFiles(toExtract, new UnpackerSettings());
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
    public Task<Dictionary<Hash, byte[]>> ExtractFiles(IEnumerable<Hash> files, CancellationToken token = default)
    {
        // Group the files by archive.
        // In almost all cases, everything will go in one archive, except for cases
        // of duplicate files between different mods.
        var filesArr = files.ToArray();
        var results = new ConcurrentDictionary<Hash, byte[]>(Environment.ProcessorCount, filesArr.Length);
        var groupedFiles = new ConcurrentDictionary<AbsolutePath, List<(Hash Hash, FileEntry FileEntry)>>(Environment.ProcessorCount, 1);
        var fileExistsCache = new ConcurrentDictionary<AbsolutePath, bool>(Environment.ProcessorCount, filesArr.Length);
        
#if DEBUG
        var processedHashes = new ConcurrentDictionary<Hash, byte>();
#endif

        Parallel.ForEach(filesArr, hash =>
        {
            #if DEBUG
            if (!processedHashes.TryAdd(hash, 0))
                throw new Exception($"Duplicate hash found: {hash.ToHex()}");
            #endif

            if (TryGetLocation(hash, fileExistsCache, out var archivePath, out var fileEntry))
            {
                var group = groupedFiles.GetOrAdd(archivePath, _ => new List<(Hash, FileEntry)>());
                lock (group)
                {
                    group.Add((hash, fileEntry));
                }
            }
            else
            {
                throw new Exception($"Missing archive for {hash.ToHex()}");
            }
        });

        // Extract from all source archives.
        Parallel.ForEach(groupedFiles, group =>
        {
            var file = group.Key.Read();
            var provider = new FromStreamProvider(file);
            var unpacker = new NxUnpacker(provider);

            var toExtract = new IOutputDataProvider[group.Value.Count];
            for (var i = 0; i < group.Value.Count; i++)
            {
                var entry = group.Value[i];
                toExtract[i] = new OutputArrayProvider("", entry.FileEntry);
            }

            unpacker.ExtractFiles(toExtract, new UnpackerSettings());
            for (var i = 0; i < group.Value.Count; i++)
            {
                var hash = group.Value[i].Hash;
                var output = (OutputArrayProvider)toExtract[i];
                results.TryAdd(hash, output.Data);
            }
        });

        return Task.FromResult(new Dictionary<Hash, byte[]>(results));
    }

    /// <inheritdoc />
    public Task<Stream> GetFileStream(Hash hash, CancellationToken token = default)
    {
        if (hash == Hash.Zero)
            throw new ArgumentNullException(nameof(hash));
        if (!TryGetLocation(hash, null, out var archivePath, out var entry))
            throw new Exception($"Missing archive for {hash.ToHex()}");

        var file = archivePath.Read();

        var provider = new FromStreamProvider(file);
        var header = HeaderParser.ParseHeader(provider);

        return Task.FromResult<Stream>(
            new ChunkedStream<ChunkedArchiveStream>(new ChunkedArchiveStream(entry, header, file)));
    }

    /// <inheritdoc />
    public unsafe HashSet<ulong> GetFileHashes()
    {
        // Build a Hash Table of all currently known files. We do this to deduplicate files between downloads.
        var fileHashes = new HashSet<ulong>();
        foreach (var arcFile in _store.GetAll<ArchivedFile>(EntityCategory.ArchivedFiles)!)
        {
            fixed (byte* ptr = arcFile.FileEntryData.AsSpan())
            {
                var reader = new LittleEndianReader(ptr);
                fileHashes.Add(reader.ReadUlongAtOffset(8)); // Hash. Offset 8 in V1 header, per spec.
            }
        }

        return fileHashes;
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
            var extractable =
                PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize, out var offset);
            _stream.Position = offset;
            using var compressedBlock = MemoryPool<byte>.Shared.Rent(compressedBlockSize);
            await _stream.ReadExactlyAsync(compressedBlock.Memory[..compressedBlockSize], token);
            ProcessBlock(buffer.Span, blockIndex, extractable, compressedBlock.Memory.Span, compressedBlockSize);
        }

        public void ReadChunk(Span<byte> buffer, ulong localIndex)
        {
            var extractable =
                PreProcessBlock(localIndex, out var blockIndex, out var compressedBlockSize, out var offset);
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
        private ExtractableBlock PreProcessBlock(ulong localIndex, out int blockIndex, out int compressedBlockSize,
            out long offset)
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
        private unsafe void ProcessBlock(Span<byte> buffer, int blockIndex, ExtractableBlock extractable,
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
                    DecompressSize = _entry.DecompressedBlockOffset +
                                     (int)Math.Min(remainingDecompSize, (ulong)chunkSize)
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

    private unsafe bool TryGetLocation(Hash hash, ConcurrentDictionary<AbsolutePath, bool>? existsCache, out AbsolutePath archivePath, out FileEntry fileEntry)
    {
        var key = new Id64(EntityCategory.ArchivedFiles, (ulong)hash);
        var item = _store.Get<ArchivedFile>(key);
        if (item != null)
        {
            foreach (var location in _archiveLocations)
            {
                var path = location.Combine(item.File);
                var exists = existsCache?.GetOrAdd(path, path.FileExists) ?? path.FileExists;
                if (!exists) 
                    continue;

                archivePath = path;

                fixed (byte* ptr = item.FileEntryData.AsSpan())
                {
                    var reader = new LittleEndianReader(ptr);
                    FileEntry tmpEntry = default;

                    tmpEntry.FromReaderV1(ref reader);
                    fileEntry = tmpEntry;
                    return true;
                }
            }
        }

        archivePath = default(AbsolutePath);
        fileEntry = default(FileEntry);
        return false;
    }
}
