using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.FileExtractor.StreamFactories;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class FileContentsCache
{
    private readonly ILogger<FileContentsCache> _logger;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly TemporaryFileManager _manager;
    private readonly IResource<FileContentsCache, Size> _limiter;
    private readonly SignatureChecker _sigs;
    private readonly IDataStore _store;
    private readonly FileHashCache _fileHashCahce;
    private readonly ILookup<FileType, IFileAnalyzer> _analyzers;

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
        _fileHashCahce = hashCache;
    }

    public async Task<AnalyzedFile> AnalyzeFile(AbsolutePath path, CancellationToken token = default)
    {
        var entry = await _fileHashCahce.HashFileAsync(path, token);
        var found = _store.Get<AnalyzedFile>(new Id64(EntityCategory.FileAnalysis, (ulong)entry.Hash));
        if (found != null) return found;

        var result = await AnalyzeFileInner(new NativeFileStreamFactory(path), token);
        result.EnsureStored();
        return result;
    }

    public AnalyzedFile? GetAnalysisData(Hash hash)
    {
        return _store.Get<AnalyzedFile>(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
    }

    private Task<AnalyzedFile> AnalyzeFileInner(IStreamFactory sFn, CancellationToken token = default)
    {
        return AnalyzeFileInner(sFn, token, 0, Hash.Zero, default);
    }

    private async Task<AnalyzedFile> AnalyzeFileInner(IStreamFactory sFn, CancellationToken token, int level, Hash parent, RelativePath parentPath)
    {
        Hash hash = default;
        var sigs = new List<FileType>();
        var analysisData = new List<IFileAnalysisData>();
        {
            await using var hashStream = await sFn.GetStreamAsync();
            if (level == 0)
            {
                if (sFn.Name is AbsolutePath ap)
                {
                    hash = (await _fileHashCahce.HashFileAsync(ap, token)).Hash;
                }
                else
                {
                    using var job = await _limiter.Begin($"Hashing {sFn.Name.FileName}", sFn.Size, token);
                    hash = await hashStream.Hash(token, job);
                }
            }
            else
            {
                hash = await hashStream.Hash(token);
            }


            var found = _store.Get<Entity>(new Id64(EntityCategory.FileAnalysis, (ulong)hash));
            if (found is AnalyzedFile af) return af;

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
                        await foreach (var data in analyzer.AnalyzeAsync(hashStream, token))
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
            file = await AnalyzeArchiveInner(sFn, level, hash, sigs, analysisData, token) ?? default;
        }

        file ??= new AnalyzedFile
        {
            Hash = hash,
            Size = sFn.Size,
            FileTypes = sigs.ToArray(),
            AnalysisData = analysisData.ToImmutableList(),
            Store = _store
        };

        if (parent != Hash.Zero)
            EnsureReverseIndex(hash, parent, parentPath);


        return file;
    }

    private async Task<AnalyzedFile?> AnalyzeArchiveInner(IStreamFactory sFn, int level, Hash hash, List<FileType> sigs,
        List<IFileAnalysisData> analysisData, CancellationToken token)
    {
        try
        {
            AnalyzedFile file;
            await using var tmpFolder = _manager.CreateFolder();
            List<KeyValuePair<RelativePath, IId>> children;
            {
                await _extractor.ExtractAllAsync(sFn, tmpFolder, token);
                children = await _limiter.ForEachFile(tmpFolder,
                        async (_, entry) =>
                        {
                            var relPath = entry.Path.RelativeTo(tmpFolder.Path);
                            return (entry.Path,
                                Results: await AnalyzeFileInner(new NativeFileStreamFactory(entry.Path), token,
                                    level + 1, hash, relPath));
                        },
                        token, "Analyzing Files")
                    .Select(a => KeyValuePair.Create(a.Path.RelativeTo(tmpFolder.Path), a.Results.DataStoreId))
                    .ToList();
            }
            file = new AnalyzedArchive
            {
                Hash = hash,
                Size = sFn.Size,
                FileTypes = sigs.ToArray(),
                AnalysisData = analysisData.ToImmutableList(),
                Contents = new EntityDictionary<RelativePath, AnalyzedFile>(_store, children),
                Store = _store
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
            Path = parentPath,
            Store = _store
        };
        entity.EnsureStored();
    }

    public IEnumerable<FileContainedIn> ArchivesThatContain(Hash hash)
    {
        var prefix = new Id64(EntityCategory.FileContainedIn, (ulong)hash);
        return _store.GetByPrefix<FileContainedIn>(prefix);
    }
}
