using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Archives.Nx.FileProviders;
using NexusMods.Archives.Nx.Headers;
using NexusMods.Archives.Nx.Headers.Managed;
using NexusMods.Archives.Nx.Interfaces;
using NexusMods.Archives.Nx.Packing;
using NexusMods.Archives.Nx.Packing.Unpack;
using NexusMods.Archives.Nx.Structs;
using NexusMods.Archives.Nx.Utilities;
using NexusMods.Hashing.xxHash3;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.Sdk.Hashes;
using NexusMods.Sdk.Threading;
using System.Diagnostics;
using NexusMods.Sdk.FileStore;
using NexusMods.Sdk.IO;

namespace NexusMods.DataModel;

/// <summary>
/// A IFileStore implementation that uses the Nx format for storage.
/// </summary>
public class NxFileStore : IFileStore
{
    private readonly AsyncFriendlyReaderWriterLock _lock = new(); // See details on struct.
    private readonly AbsolutePath[] _archiveLocations;
    private readonly IConnection _conn;
    private readonly ILogger<NxFileStore> _logger;
    private FrozenDictionary<Hash, ArchiveContents> _archivesByEntry = FrozenDictionary<Hash, ArchiveContents>.Empty;

    private record ArchiveContents(AbsolutePath ArchivePath, FrozenDictionary<Hash, FileEntry> Entries);
    
    /// <summary>
    /// This is the hash of an empty byte sequence. Useful for determining if we're being asked
    /// to read an empty file (which is never going to be archived). 
    /// </summary>
    private static readonly Hash EmptyFile = Array.Empty<byte>().xxHash3();

    /// <summary>
    /// Constructor
    /// </summary>
    public NxFileStore(
        ILogger<NxFileStore> logger,
        IConnection conn,
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
        _conn = conn;
        ReloadCaches();
    }

    public void ReloadCaches()
    {
        var archives = _archiveLocations
            .SelectMany(folder => folder.EnumerateFiles(KnownExtensions.Nx))
            .AsParallel()
            .Select(file =>
                {
                    using var stream = file.Read();
                    var provider = new FromStreamProvider(stream);
                    var header = HeaderParser.ParseHeader(provider);
                    Dictionary<Hash, FileEntry> entries = new();
                    foreach (var entry in header.Entries)
                        entries[Hash.From(entry.Hash)] = entry;
                    return new ArchiveContents(file, entries.ToFrozenDictionary());
                }
            ).ToList();
        var index = new Dictionary<Hash, ArchiveContents>();
        foreach (var archive in archives)
        {
            foreach (var entry in archive.Entries)
            {
                index.Add(entry.Key, archive);
            }
        }
        _archivesByEntry = index.ToFrozenDictionary();
    }

    /// <inheritdoc />
    public ValueTask<bool> HaveFile(Hash hash) => ValueTask.FromResult(_archivesByEntry.ContainsKey(hash));

    /// <inheritdoc />
    public async Task BackupFiles(IEnumerable<ArchivedFileEntry> backups, bool deduplicate = true, CancellationToken token = default)
    {
        var builder = new NxPackerBuilder();
        var distinct = backups.DistinctBy(d => d.Hash).ToArray();

        if (distinct.Length == 0)
            return;
        
        var streams = new List<Stream>();
        foreach (var backup in distinct)
        {
            if (deduplicate && await HaveFile(backup.Hash))
                continue;

            var stream = await backup.StreamFactory.GetStreamAsync();
            streams.Add(stream);
            builder.AddFile(stream, new AddFileParams
            {
                RelativePath = backup.Hash.ToHex(),
            });
        }

        _logger.LogDebug("Backing up {Count} files of {Size} in size", distinct.Length, distinct.Sum(s => s.Size));
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
        ReloadCaches();
    }

    /// <inheritdoc />
    public Task BackupFiles(string archiveName, IEnumerable<ArchivedFileEntry> files, CancellationToken cancellationToken = default)
    {
        // TODO: implement with repacking
        return BackupFiles(files, deduplicate: true, cancellationToken);
    }
    
    /// <inheritdoc />
    public async Task ExtractFiles(IEnumerable<(Hash Hash, AbsolutePath Dest)> files, CancellationToken token = default)
    {
        using var lck = _lock.ReadLock();

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
            // Create empty files as empty
            if (file.Hash == EmptyFile)
            {
                file.Dest.Create().Dispose();
                return;
            }

            if (TryGetLocation(_conn.Db, file.Hash, fileExistsCache,
                    out var archivePath, out var fileEntry))
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
                throw new MissingArchiveException(file.Hash, file.Dest);
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
                _logger.LogError(e, "Failed to extract files");
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
        using var lck = _lock.ReadLock();

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
            if (hash == EmptyFile)
            {
                results.TryAdd(hash, []);
                return;
            }
#if DEBUG
            if (!processedHashes.TryAdd(hash, 0))
                throw new Exception($"Duplicate hash found: {hash.ToHex()}");
#endif

            if (TryGetLocation(_conn.Db, hash, fileExistsCache,
                    out var archivePath, out var fileEntry))
            {
                var group = groupedFiles.GetOrAdd(archivePath, _ => new List<(Hash, FileEntry)>());
                lock (group)
                {
                    group.Add((hash, fileEntry));
                }
            }
            else
            {
                throw new MissingArchiveException(hash);
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
        {
            return Task.FromResult(Stream.Null);
        }

        using var lck = _lock.ReadLock();
        if (!TryGetLocation(_conn.Db, hash, null,
                out var archivePath, out var entry))
            throw new MissingArchiveException(hash);

        var file = archivePath.Read();

        var provider = new FromStreamProvider(file);
        var header = HeaderParser.ParseHeader(provider);

        return Task.FromResult<Stream>(
            new ChunkedStream<ChunkedArchiveStream>(new ChunkedArchiveStream(entry, header, file)));
    }

    public Task<byte[]> Load(Hash hash, CancellationToken token = default)
    {
        if (hash == Hash.Zero)
            throw new ArgumentNullException(nameof(hash));

        using var lck = _lock.ReadLock();
        if (!TryGetLocation(_conn.Db, hash, null,
                out var archivePath, out var entry))
            throw new MissingArchiveException(hash);

        var file = archivePath.Read();

        var provider = new FromStreamProvider(file);
        var unpacker = new NxUnpacker(provider);

        var output = new OutputArrayProvider("", entry);

        unpacker.ExtractFiles([output], new UnpackerSettings()
        {
            MaxNumThreads = 1,
        });

        return Task.FromResult(output.Data);
    }
    
    /// <inheritdoc />
    public AsyncFriendlyReaderWriterLock.WriteLockDisposable Lock() => _lock.WriteLock();
    

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
        private ExtractableBlock PreProcessBlock(ulong localIndex, out int blockIndex, out int compressedBlockSize,
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
                var block = new ExtractableBlock
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

    internal bool TryGetLocation(IDb db, Hash hash, ConcurrentDictionary<AbsolutePath, bool>? existsCache, out AbsolutePath archivePath, out FileEntry fileEntry)
    {
        archivePath = default(AbsolutePath);
        fileEntry = default(FileEntry);
        
        if (!_archivesByEntry.TryGetValue(hash, out var archive))
            return false;
        
        archivePath = archive.ArchivePath;
        if (!archive.Entries.TryGetValue(hash, out fileEntry))
            throw new KeyNotFoundException("Missing file entry: " + hash.ToHex() + "this should never happen");
        return true;
    }
}
