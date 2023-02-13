using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

public class ArchiveManager
{
    private readonly IDataStore _store;
    private readonly ILogger<ArchiveManager> _logger;
    private readonly HashSet<AbsolutePath> _locations;
    private readonly FileExtractor.FileExtractor _fileExtractor;
    private readonly FileContentsCache _contentsCache;

    public ArchiveManager(ILogger<ArchiveManager> logger, IEnumerable<AbsolutePath> locations, IDataStore store, 
        FileExtractor.FileExtractor fileExtractor, FileContentsCache contentsCache)
    {
        _logger = logger;
        _locations = locations.ToHashSet();
        _store = store;
        _contentsCache = contentsCache;
        _fileExtractor = fileExtractor;
        foreach (var folder in _locations)
            folder.CreateDirectory();
    }

    public async Task<Hash> ArchiveFile(AbsolutePath path, CancellationToken token = default, IJob<Size>? job = null)
    {
        Hash hash;
        var folder = SelectLocation(path);
        var tmpName = folder.Join(Guid.NewGuid().ToString().ToRelativePath().WithExtension(KnownExtensions.Tmp));
        {
            await using var tmpFile = tmpName.Create();
            await using var src = path.Read();
            hash = await src.HashingCopy(tmpFile, token, job);
        }
        var finalName = folder.Join(NameForHash(hash));
        if (!finalName.FileExists) 
            await tmpName.MoveToAsync(finalName);
        return hash;
    }

    private static RelativePath NameForHash(Hash hash)
    {
        return hash.ToHex().ToRelativePath().WithExtension(KnownExtensions.Ra);
    }

    public Stream Open(Hash hash)
    {
        return PathFor(hash).Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    public AbsolutePath PathFor(Hash hash)
    {
        var rel = NameForHash(hash);
        return _locations.Select(r => r.Join(rel))
            .First(r => r.FileExists);
    }

    public bool HaveArchive(Hash hash)
    {
        var rel = NameForHash(hash);
        return _locations.Any(r => r.Join(rel).FileExists);
    }

    /// <summary>
    /// Returns true if the given hash is managed, or any archive that contains this file is managed
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public bool HaveFile(Hash hash)
    {
        if (HaveArchive(hash)) return true;

        return _contentsCache.ArchivesThatContain(hash).Any(containedIn => HaveFile(containedIn.Parent));
    }
    
    /// <summary>
    /// Returns true if the given hash is managed, or any archive that contains this file is managed
    /// </summary>
    /// <param name="hash"></param>
    /// <returns></returns>
    public IEnumerable<HashRelativePath> ArchivesThatContain(Hash hash)
    {
        if (HaveArchive(hash)) return new[]{ new HashRelativePath(hash, Array.Empty<RelativePath>())};
        return _contentsCache.ArchivesThatContain(hash).Select(r => new HashRelativePath(r.Parent, r.Path));
    }

    private AbsolutePath SelectLocation(AbsolutePath path)
    {
        return _locations.First();
    }

    public HashSet<Hash> AllArchives()
    {
        return _locations.SelectMany(e => e.EnumerateFiles(KnownExtensions.Ra))
            .Select(a => Hash.FromHex(a.FileName.FileNameWithoutExtension.ToString()))
            .ToHashSet();
    }

    public async Task Extract(Hash hash, IEnumerable<RelativePath> select, Func<RelativePath, IStreamFactory, ValueTask> action, CancellationToken token = default)
    {
        var paths = select.ToHashSet();
        await _fileExtractor.ForEachEntry(PathFor(hash), async (path, sFn) =>
        {
            if (paths.Contains(path))
                await action(path, sFn);
            return path;
        }, token);
    }
}