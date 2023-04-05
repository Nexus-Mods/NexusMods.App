using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.RateLimiting.Extensions;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Helper method that allows you to index (analyze) files using provided <see cref="IFileAnalyzer"/>(s),
/// caching the results inside the given <see cref="IDataStore"/>.
/// </summary>
public class FileContentsCache
{
    private readonly ILogger<FileContentsCache> _logger;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly TemporaryFileManager _manager;
    private readonly IResource<FileContentsCache, Size> _limiter;
    private readonly SignatureChecker _sigs;
    private readonly IDataStore _store;
    private readonly FileHashCache _fileHashCache;
    private readonly ILookup<FileType, IFileAnalyzer> _analyzers;

    /// <summary/>
    /// <remarks>Called from DI container.</remarks>
    public FileContentsCache(ILogger<FileContentsCache> logger,
        IResource<FileContentsCache, Size> limiter,
        FileExtractor.FileExtractor extractor,
        TemporaryFileManager manager,
        FileHashCache hashCache,
        IEnumerable<IFileAnalyzer> analyzers,
        IDataStore dataStore)
    {
        _logger = logger;
        _limiter = limiter;
        _extractor = extractor;
        _manager = manager;
        _sigs = new SignatureChecker(Enum.GetValues<FileType>());
        _analyzers = analyzers.SelectMany(a => a.FileTypes.Select(t => (Type: t, Analyzer: a)))
            .ToLookup(k => k.Type, v => v.Analyzer);
        _store = dataStore;
        _fileHashCache = hashCache;
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
        if (found != null) return found;

        var result = await AnalyzeFileInnerAsync(new NativeFileStreamFactory(path), path.FileName, token);
        result.EnsurePersisted(_store);
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

            var found = _store.Get<Entity>(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
            if (found is AnalyzedFile af)
                return af;

            hashStream.Position = 0;
            sigs = (await _sigs.MatchesAsync(hashStream)).ToList();

            if (parentPath != default && SignatureChecker.TryGetFileType(parentPath.Extension, out var type))
                sigs.Add(type);

            foreach (var sig in sigs)
            {
                foreach (var analyzer in _analyzers[sig])
                {
                    hashStream.Position = 0;
                    try
                    {
                        await foreach (var data in analyzer.AnalyzeAsync(new FileAnalyzerInfo()
                                       {
                                           RelativePath = parentPath,
                                           FileName = fileName,
                                           ParentArchive = parentArchivePath,
                                           Stream = hashStream
                                       }, token))
                            analysisData.Add(data);
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
            Size = sFn.Size,
            FileTypes = sigs.ToArray(),
            AnalysisData = analysisData.ToImmutableList()
        };

        if (parent != Hash.Zero)
            EnsureReverseIndex(hash, parent, parentPath);

        return file;
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
            }

            var file = new AnalyzedArchive
            {
                Hash = hash,
                Size = sFn.Size,
                FileTypes = sigs.ToArray(),
                AnalysisData = analysisData.ToImmutableList(),
                Contents = new EntityDictionary<RelativePath, AnalyzedFile>(_store, children)
            };

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting archive {Path}, skipping analyis", sFn.Name);
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
