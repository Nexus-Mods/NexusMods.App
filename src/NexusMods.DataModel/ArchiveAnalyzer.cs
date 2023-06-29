using System.Buffers.Binary;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.ArchiveMetaData;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.RateLimiting.Extensions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel;

/// <summary>
/// Helper method that allows you to index (analyze) files using provided <see cref="IFileAnalyzer"/>(s),
/// caching the results inside the given <see cref="IDataStore"/>.
/// </summary>
public class ArchiveAnalyzer : IArchiveAnalyzer
{
    private readonly ILogger<ArchiveAnalyzer> _logger;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly TemporaryFileManager _manager;
    private readonly IResource<ArchiveAnalyzer, Size> _limiter;
    private readonly SignatureChecker _sigs;
    private readonly IDataStore _store;
    private readonly FileHashCache _fileHashCache;
    private readonly ILookup<FileType, IFileAnalyzer> _analyzers;
    private readonly IArchiveManager _archiveManager;

    /// <summary>
    /// The signature of the analyzers used when indexing files.
    /// </summary>
    public Hash AnalyzersSignature { get; private set; }

    /// <summary/>
    /// <remarks>Called from DI container.</remarks>
    public ArchiveAnalyzer(ILogger<ArchiveAnalyzer> logger,
        IResource<ArchiveAnalyzer, Size> limiter,
        FileExtractor.FileExtractor extractor,
        TemporaryFileManager manager,
        FileHashCache hashCache,
        IEnumerable<IFileAnalyzer> analyzers,
        IDataStore dataStore,
        IArchiveManager archiveManager)
    {
        _logger = logger;
        _limiter = limiter;
        _extractor = extractor;
        _manager = manager;
        _sigs = new SignatureChecker(Enum.GetValues<FileType>());
        _analyzers = analyzers.SelectMany(a => a.FileTypes.Select(t => (Type: t, Analyzer: a)))
            .ToLookup(k => k.Type, v => v.Analyzer);
        AnalyzersSignature = MakeAnalyzerSignature(analyzers);
        _store = dataStore;
        _fileHashCache = hashCache;
        _archiveManager = archiveManager;
    }

    /// <summary>
    /// Recalculates the analyzer signature, only used for testing.
    /// </summary>
    public void RecalculateAnalyzerSignature()
    {
        AnalyzersSignature =
            MakeAnalyzerSignature(_analyzers.SelectMany(x => x).Distinct());
    }

    /// <summary>
    /// Analyzes a file and caches the result within the data store.
    /// </summary>
    /// <param name="path">Path of the file to be analyzed.</param>
    /// <param name="token">Allows you to cancel the operation.</param>
    /// <returns>The file analysis data.</returns>
    public async Task<AnalyzedFile> AnalyzeFileAsync(AbsolutePath path, CancellationToken token = default)
    {
        var entry = await _fileHashCache.IndexFileAsync(path, token);
        var found = _store.Get<AnalyzedFile>(new Id64(EntityCategory.FileAnalysis, (ulong)entry.Hash));
        if (found != null && found.AnalyzersHash == AnalyzersSignature)
        {
            if (found is AnalyzedArchive aa && !AArchiveMetaData.GetMetaDatas(_store, found.Hash).OfType<FileArchiveMetaData>().Any())
            {
                var metaData = FileArchiveMetaData.Create(path, aa);
                metaData.EnsurePersisted(_store);
            }
            return found;
        }

        // Analyze the archive and cache the info
        var result = await AnalyzeFileInnerAsync(new NativeFileStreamFactory(path), path.FileName, token);
        result.EnsurePersisted(_store);

        // Save the source of this archive so we can use it later
        if (result is AnalyzedArchive archive)
        {
            var metaData = FileArchiveMetaData.Create(path, archive);
            metaData.EnsurePersisted(_store);
        }

        return result;
    }

    /// <summary>
    /// Retrieves all archives that contain a file with a specific hash.
    /// </summary>
    /// <param name="hash">The hash of the file inside the archive.</param>
    /// <returns>All matching archives.</returns>
    public IEnumerable<FileContainedIn> ArchivesThatContain(Hash hash)
    {
        var prefix = new Id64(EntityCategory.FileContainedIn, (ulong)hash);
        return _store.GetByPrefix<FileContainedIn>(prefix);
    }

    /// <summary>
    /// Gets the file analysis result for a file with a given hash.
    ///
    /// This file can either be an archive or a file stored within
    /// an archive for a given file.
    /// </summary>
    /// <param name="hash">The hash of the file for which data is to be obtained.</param>
    /// <returns></returns>
    public AnalyzedFile? GetAnalysisData(Hash hash)
    {
        return _store.Get<AnalyzedFile>(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
    }

    private Task<AnalyzedFile> AnalyzeFileInnerAsync(IStreamFactory sFn, string fileName, CancellationToken token = default)
    {
        return AnalyzeFileInnerAsync(sFn, token, 0, Hash.Zero, null, default, fileName);
    }

    private async Task<AnalyzedFile> AnalyzeFileInnerAsync(IStreamFactory sFn, CancellationToken token, int level, Hash parent, TemporaryPath? parentArchivePath, RelativePath parentPath, string fileName)
    {
        Hash hash;
        List<FileType> sigs;
        var analysisData = new List<IFileAnalysisData>();
        {
            await using var hashStream = await sFn.GetStreamAsync();
            if (level == 0)
            {
                if (sFn.Name is AbsolutePath ap)
                {
                    hash = (await _fileHashCache.IndexFileAsync(ap, token)).Hash;
                }
                else
                {
                    using var job = await _limiter.BeginAsync($"Hashing {sFn.Name.FileName}", sFn.Size, token);
                    hash = await hashStream.XxHash64Async(token, job);
                }
            }
            else
            {
                hash = await hashStream.XxHash64Async(token);
            }

            hashStream.Position = 0;
            sigs = (await _sigs.MatchesAsync(hashStream)).ToList();

            if (parentPath != default && SignatureChecker.TryGetFileType(parentPath.Extension, out var type))
                sigs.Add(type);

            var found = _store.Get<Entity>(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
            if (found is AnalyzedFile af && af.AnalyzersHash == AnalyzersSignature)
                return af;

            hashStream.Position = 0;


            foreach (var sig in sigs)
            {
                foreach (var analyzer in _analyzers[sig])
                {
                    hashStream.Position = 0;
                    try
                    {
                        var fileAnalyzerInfo = new FileAnalyzerInfo
                        {

                            RelativePath = parentPath,
                            FileName = fileName,
                            ParentArchive = parentArchivePath,
                            Stream = hashStream
                        };

                        await foreach (var data in analyzer.AnalyzeAsync(fileAnalyzerInfo, token))
                        {
                            analysisData.Add(data);
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "Error analyzing {Path} with {Analyzer}", sFn.Name, analyzer.GetType().Name);
                    }
                }
            }
        }

        AnalyzedFile? file = null;

        if (await _extractor.CanExtract(sFn))
        {
            file = await AnalyzeArchiveInnerAsync(sFn, level, hash, sigs, analysisData, token) ?? default;
        }

        file ??= new AnalyzedFile
        {
            Hash = hash,
            AnalyzersHash = AnalyzersSignature,
            Size = sFn.Size,
            FileTypes = sigs.ToArray(),
            AnalysisData = analysisData.ToImmutableList()
        };

        if (parent != Hash.Zero)
            EnsureReverseIndex(hash, parent, parentPath);

        return file;
    }

    private Hash MakeAnalyzerSignature(IEnumerable<IFileAnalyzer> analyzers)
    {
        var algo = new XxHash64Algorithm(0);
        // We have to hash blocks in at least 32 bytes at a time. We'll waste a bit of space here
        // but at least we don't have to allocate a MemoryStream and pour the data into it.
        Span<byte> buffer = stackalloc byte[32];
        foreach (var analyzer in analyzers.OrderBy(a => a.Id.Analyzer))
        {
            analyzer.Id.Analyzer.TryWriteBytes(buffer);
            BinaryPrimitives.WriteUInt32BigEndian(buffer.SliceFast(16), analyzer.Id.Revision);
            algo.TransformByteGroupsInternal(buffer);
        }

        return Hash.FromULong(algo.FinalizeHashValueInternal(Span<byte>.Empty));
    }

    private async Task<AnalyzedFile?> AnalyzeArchiveInnerAsync(IStreamFactory sFn, int level, Hash hash, List<FileType> sigs,
        List<IFileAnalysisData> analysisData, CancellationToken token)
    {
        try
        {
            await using var tmpFolder = _manager.CreateFolder();
            List<KeyValuePair<RelativePath, IId>> children;
            {
                await _extractor.ExtractAllAsync(sFn, tmpFolder, token);
                children = await _limiter.ForEachFileAsync(tmpFolder,
                        async (_, entry) =>
                        {
                            // ReSharper disable once AccessToDisposedClosure
                            var relPath = entry.Path.RelativeTo(tmpFolder.Path);

                            // ReSharper disable once AccessToDisposedClosure
                            var analysisRecord = await AnalyzeFileInnerAsync(
                                new NativeFileStreamFactory(entry.Path), token,
                                level + 1, hash, tmpFolder, relPath, relPath.FileName);
                            analysisRecord.WithPersist(_store);
                            return (entry.Path,
                                Results: analysisRecord);
                        },
                        token, "Analyzing Files")
                    // ReSharper disable once AccessToDisposedClosure
                    .SelectAsync(a => KeyValuePair.Create(a.Path.RelativeTo(tmpFolder.Path), a.Results.DataStoreId))
                    .ToListAsync();

                // Some parts of this code fail with an empty collection
                if (children.Any())
                {
                    await _archiveManager.BackupFiles(children
                        .Select(c =>
                        {
                            IStreamFactory path = new NativeFileStreamFactory(tmpFolder.Path.Combine(c.Key));
                            var analysis = _store.Get<AnalyzedFile>(c.Value)!;
                            return (path, analysis.Hash, analysis.Size);
                        }), token);
                }
            }

            var file = new AnalyzedArchive
            {
                Hash = hash,
                AnalyzersHash = AnalyzersSignature,
                Size = sFn.Size,
                FileTypes = sigs.ToArray(),
                AnalysisData = analysisData.ToImmutableList(),
                Contents = new EntityDictionary<RelativePath, AnalyzedFile>(_store, children)
            };

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting archive {Path}, skipping analysis", sFn.Name);
            return null;
        }
    }

    private void EnsureReverseIndex(Hash hash, Hash parent, RelativePath parentPath)
    {
        var entity = new FileContainedIn
        {
            File = hash,
            Parent = parent,
            Path = parentPath
        };
        entity.EnsurePersisted(_store);
    }
}
