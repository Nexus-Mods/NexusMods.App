﻿using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Hashing.xxHash64;
using NexusMods.Interfaces.Streams;
using NexusMods.Paths;

namespace NexusMods.DataModel;

public class ArchiveManager
{
    private readonly IDataStore _store;
    private readonly ILogger<ArchiveManager> _logger;
    private readonly HashSet<AbsolutePath> _locations;
    private readonly FileExtractor.FileExtractor _fileExtractor;

    public ArchiveManager(ILogger<ArchiveManager> logger, IEnumerable<AbsolutePath> locations, IDataStore store, FileExtractor.FileExtractor fileExtractor)
    {
        _logger = logger;
        _locations = locations.ToHashSet();
        _store = store;
        _fileExtractor = fileExtractor;
        foreach (var folder in _locations)
            folder.CreateDirectory();
    }

    public async Task<Hash> ArchiveFile(AbsolutePath path, CancellationToken token = default, IJob<Size>? job = null)
    {
        Hash hash;
        var folder = SelectLocation(path);
        var tmpName = folder.Combine(Guid.NewGuid().ToString().ToRelativePath().WithExtension(Ext.Tmp));
        {
            await using var tmpFile = tmpName.Create();
            await using var src = path.Read();
            hash = await src.HashingCopy(tmpFile, token, job);
        }
        var finalName = folder.Combine(NameForHash(hash));
        if (!finalName.FileExists) 
            await tmpName.MoveToAsync(finalName);
        return hash;
    }

    private static RelativePath NameForHash(Hash hash)
    {
        return hash.ToHex().ToRelativePath().WithExtension(Ext.Ra);
    }

    public async Task<Stream> Open(Hash hash)
    {
        return PathFor(hash).Open(FileMode.Open, FileAccess.Read, FileShare.Read);
    }

    private AbsolutePath PathFor(Hash hash)
    {
        var rel = NameForHash(hash);
        return _locations.Select(r => r.Combine(rel))
            .First(r => r.FileExists);
    }

    public bool HaveArchive(Hash hash)
    {
        var rel = NameForHash(hash);
        return _locations.Any(r => r.Combine(rel).FileExists);
    }

    private AbsolutePath SelectLocation(AbsolutePath path)
    {
        return _locations.First();
    }

    public HashSet<Hash> AllArchives()
    {
        return _locations.SelectMany(e => e.EnumerateFiles(Ext.Ra))
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