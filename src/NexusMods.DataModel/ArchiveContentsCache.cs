using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.ArchiveContents;
using NexusMods.DataModel.Extensions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.FileSignatures;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces.Streams;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class ArchiveContentsCache
{
    private readonly ILogger<ArchiveContentsCache> _logger;
    private readonly FileExtractor.FileExtractor _extractor;
    private readonly TemporaryFileManager _manager;
    private readonly IResource<ArchiveContentsCache,Size> _limiter;
    private readonly SignatureChecker _sigs;
    private readonly IDataStore _store;
    private readonly FileHashCache _fileHashCahce;

    public ArchiveContentsCache(ILogger<ArchiveContentsCache> logger, 
        IResource<ArchiveContentsCache, Size> limiter,  
        FileExtractor.FileExtractor extractor, 
        TemporaryFileManager manager,
        FileHashCache hashCache,
        IDataStore dataStore)
    {
        _logger = logger;
        _limiter = limiter;
        _extractor = extractor;
        _manager = manager;
        _sigs = new SignatureChecker(Enum.GetValues<FileType>());
        _store = dataStore;
        _fileHashCahce = hashCache;
    }

    public async Task<AnalyzedFile> AnalyzeFile(AbsolutePath path, CancellationToken token = default)
    {
        var result = await AnalyzeFileInner(new NativeFileStreamFactory(path), token);
        result.EnsureStored();
        return result;
    }

    public async Task<AnalyzedFile> AnalyzeFileInner(IStreamFactory sFn, CancellationToken token = default, int level = 0, Hash parent = default, RelativePath parentPath = default)
    {
        Hash hash = default;
        var sigs = Array.Empty<FileType>();
        {
            await using var hashStream = await sFn.GetStream();
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

            var found = _store.Get<Entity>(new Id64(EntityCategory.FileAnalysis, hash));
            if (found is AnalyzedFile af) return af;
            
            hashStream.Position = 0;
            sigs = (await _sigs.MatchesAsync(hashStream)).ToArray();
        }

        AnalyzedFile file;
        
        if (await _extractor.CanExtract(sFn))
        {
            await using var tmpFolder = _manager.CreateFolder();
            List<KeyValuePair<RelativePath, Id>> children;
            {
                await _extractor.ExtractAll(sFn, tmpFolder, token);
                children = await _limiter.ForEachFile(tmpFolder, 
                        async (job, entry) =>
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
                FileTypes = sigs,
                Contents = new EntityDictionary<RelativePath, AnalyzedFile>(_store, children),
                Store = _store
            };
        }
        else
        {
            file = new AnalyzedFile
            {
                Hash = hash,
                Size = sFn.Size,
                FileTypes = sigs,
                Store = _store
            };
        }

        if (parent != default) 
            EnsureReverseIndex(hash, parent, parentPath);
            
        return file;
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
        var prefix = new Id64(EntityCategory.FileContainedIn, hash);
        return _store.GetByPrefix<FileContainedIn>(prefix);
    }
}