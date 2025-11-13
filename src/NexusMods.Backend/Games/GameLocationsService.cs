using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.IO.Hashing;
using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games.FileHashes;
using NexusMods.Abstractions.Games.FileHashes.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.Hashing.xxHash3;
using NexusMods.Hashing.xxHash3.Paths;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using NexusMods.Sdk.Games;
using NexusMods.Sdk.Hashes;

namespace NexusMods.Backend.Games;

internal class GameLocationsService : IGameLocationsService
{
    private readonly ILogger _logger;
    private readonly IFileHashesService _fileHashesService;

    public GameLocationsService(IServiceProvider serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameLocationsService>>();
        _fileHashesService = serviceProvider.GetRequiredService<IFileHashesService>();
    }

    public async Task<IndexGameResult> IndexGame(
        GameInstallation installation,
        FrozenDictionary<GamePath, DiskStateEntry.ReadOnly> previousDiskState,
        IGamePathFilter filter,
        CancellationToken outerToken = default)
    {
        var topLevelLocations = installation.Locations.GetTopLevelLocations();
        var enumerable = topLevelLocations.Where(kv => kv.Value.DirectoryExists()).SelectMany(kv => kv.Value.EnumerateFiles()).ToAsyncEnumerable();

        var seenPaths = new ConcurrentDictionary<GamePath, bool>();
        var newFiles = new ConcurrentDictionary<GamePath, IndexFileResult>();
        var modifiedFiles = new ConcurrentDictionary<GamePath, IndexFileResult>();

        await Parallel.ForEachAsync(enumerable, cancellationToken: outerToken, async (file, token) =>
        {
            try
            {
                var gamePath = installation.Locations.ToGamePath(file);
                if (filter.ShouldFilter(gamePath)) return;
                if (!seenPaths.TryAdd(gamePath, true)) return;

                if (previousDiskState.TryGetValue(gamePath, out var previousDiskStateEntry))
                {
                    if (HasFileChanged(file, previousDiskStateEntry))
                    {
                        var indexFileResult = await IndexFile(file, gamePath, token);
                        modifiedFiles[gamePath] = indexFileResult;
                    }
                }
                else
                {
                    var indexFileResult = await IndexFile(file, gamePath, token);
                    newFiles[gamePath] = indexFileResult;
                }
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Exception indexing file `{Path}`", file);
            }
        });

        var removedFiles = previousDiskState.Keys.Except(seenPaths.Keys).ToFrozenSet();
        var result = new IndexGameResult(
            NewFiles: newFiles.ToFrozenDictionary(),
            ModifiedFiles: modifiedFiles.ToFrozenDictionary(),
            RemovedFiles: removedFiles
        );

        return result;
    }

    private static bool HasFileChanged(AbsolutePath file, DiskStateEntry.ReadOnly previousState)
    {
        var fileInfo= file.FileInfo;
        var currentLastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc);
        var currentSize = fileInfo.Size;

        return currentLastModified != previousState.LastModified || currentSize != previousState.Size;
    }

    private async ValueTask<IndexFileResult> IndexFile(
        AbsolutePath file,
        GamePath gamePath,
        CancellationToken cancellationToken)
    {
        var fileInfo= file.FileInfo;
        var currentLastModified = new DateTimeOffset(fileInfo.LastWriteTimeUtc);
        var currentSize = fileInfo.Size;

        var newHash = await HashFile(file, gamePath, cancellationToken);
        return new IndexFileResult(gamePath, newHash, currentSize, currentLastModified);
    }

    private async ValueTask<Hash> HashFile(AbsolutePath file, GamePath gamePath, CancellationToken cancellationToken)
    {
        var existingHash = await GetExistingHash(hashesDb: _fileHashesService.Current, file, gamePath, cancellationToken);
        if (existingHash.HasValue) return existingHash.Value;

        _logger.LogDebug("Didn't find matching hash data for file `{Path}`, falling back to a full hash", file);
        var hash = await file.XxHash3Async(token: cancellationToken);
        return hash;
    }

    private static async ValueTask<Optional<Hash>> GetExistingHash(IDb hashesDb, AbsolutePath file, GamePath gamePath, CancellationToken cancellationToken)
    {
        var pathHashRelations = PathHashRelation.FindByPath(hashesDb, gamePath.Path);
        if (pathHashRelations.Count == 0) return Optional<Hash>.None;

        var minimalHash = await GetMinimalHash(file, cancellationToken);

        var foundHash = Optional<Hash>.None;
        foreach (var pathHashRelation in pathHashRelations)
        {
            var existingHashes = pathHashRelation.Hash;
            if (existingHashes.Size != file.FileInfo.Size) continue;
            if (existingHashes.MinimalHash != minimalHash) continue;

            if (!foundHash.HasValue)
            {
                foundHash = existingHashes.XxHash3;
                continue;
            }

            if (existingHashes.XxHash3 != foundHash) return Optional<Hash>.None;
        }

        return foundHash;
    }

    private static async ValueTask<Hash> GetMinimalHash(AbsolutePath file, CancellationToken cancellationToken)
    {
        await using var fileStream = file.Read();
        return await MinimalHash.HashAsync<Hash, XxHash3, Xx3Hasher>(fileStream, cancellationToken: cancellationToken);
    }
}
