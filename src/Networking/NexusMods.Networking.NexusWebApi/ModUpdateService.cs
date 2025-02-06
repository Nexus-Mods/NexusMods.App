using DynamicData;
using DynamicData.Kernel;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;
namespace NexusMods.Networking.NexusWebApi;

public class ModUpdateService : IModUpdateService
{
    private readonly IConnection _connection;
    private readonly INexusApiClient _nexusApiClient;
    private readonly IGameDomainToGameIdMappingCache _gameIdMappingCache;
    private readonly ILogger<ModUpdateService> _logger;
    private readonly NexusGraphQLClient _gqlClient;
    
    // Use SourceCache to maintain latest values per key
    private readonly SourceCache<KeyValuePair<NexusModsFileMetadataId, NexusModsFileMetadata.ReadOnly>, EntityId> _newestModVersionCache = new (static kv => kv.Key);
    private readonly SourceCache<KeyValuePair<NexusModsModPageMetadataId, NexusModsFileMetadata.ReadOnly[]>, EntityId> _newestModOnAnyPageCache = new (static kv => kv.Key);

    public ModUpdateService(
        IConnection connection,
        INexusApiClient nexusApiClient,
        IGameDomainToGameIdMappingCache gameIdMappingCache,
        ILogger<ModUpdateService> logger,
        NexusGraphQLClient gqlClient)
    {
        _connection = connection;
        _nexusApiClient = nexusApiClient;
        _gameIdMappingCache = gameIdMappingCache;
        _logger = logger;
        _gqlClient = gqlClient;
    }

    public async Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateModPages(CancellationToken token, bool notify = true)
    {
        // Identify all mod pages needing a refresh
        var updateCheckResult = await RunUpdateCheck.CheckForModPagesWhichNeedUpdating(
            _connection.Db, 
            _nexusApiClient, 
            _gameIdMappingCache);
        
        // Fetch the data from the Nexus servers if at least a single item needs updating.
        if (updateCheckResult.AnyItemNeedsUpdate())
        {
            using var tx = _connection.BeginTransaction();
            await RunUpdateCheck.UpdateModFilesForOutdatedPages(
                _connection.Db,
                tx,
                _logger,
                _gqlClient,
                updateCheckResult,
                token);
            await tx.Commit();
        }
        
        if (notify)
            NotifyForUpdates();

        return updateCheckResult;
    }

    /// <inheritdoc />
    public void NotifyForUpdates()
    {
        var filesInLibrary = NexusModsLibraryItem
            .All(_connection.Db)
            .Select(static libraryItem => libraryItem.FileMetadata)
            .DistinctBy(static fileMetadata => fileMetadata.Id)
            .ToDictionary(static x => x.Id, static x => x);

        var existingFileToNewerFiles = filesInLibrary
            .Select(kv =>
            {
                var newerFiles = RunUpdateCheck
                    .GetNewerFilesForExistingFile(kv.Value)
                    .Where(newFile => newFile.IsValid() && !filesInLibrary.ContainsKey(newFile.Id))
                    .ToArray();

                return new KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly[]>(kv.Value, newerFiles);
            })
            .Where(static kv => kv.Value.Length > 0)
            .ToArray();

        foreach (var kv in existingFileToNewerFiles)
        {
            var kvp = new KeyValuePair<NexusModsFileMetadataId, NexusModsFileMetadata.ReadOnly>(kv.Key, kv.Value.First());
            _newestModVersionCache.AddOrUpdate(kvp);
        }

        var modPageToNewerFiles = existingFileToNewerFiles
            .SelectMany(static kv => kv.Value)
            .DistinctBy(static fileMetadata => fileMetadata.Id)
            .GroupBy(static fileMetadata => fileMetadata.ModPageId)
            .ToDictionary(static group => group.Key, static group =>
                {
                    // Sort by date (latest to oldest), as per design.
                    // https://github.com/Nexus-Mods/NexusMods.App/pull/2559#discussion_r1934250741
                    var itemsArray = group.ToArray();
                    Array.Sort(itemsArray, (a, b) => b.UploadedAt.CompareTo(a.UploadedAt));
                    return itemsArray;
                }
            );

        foreach (var kv in modPageToNewerFiles)
        {
            _newestModOnAnyPageCache.AddOrUpdate(kv);
        }
    }

    /// <inheritdoc />
    public IObservable<Optional<NexusModsFileMetadata.ReadOnly>> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current)
    {
        return _newestModVersionCache.Connect()
            .Transform(kv => kv.Value)
            .QueryWhenChanged(query => query.Lookup(current.Id));
    }

    /// <inheritdoc />
    public IObservable<Optional<NewerFilesOnModPage>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current)
    {
        return _newestModOnAnyPageCache.Connect()
            .Transform(kv => kv.Value)
            .QueryWhenChanged(query =>
                {
                    var files = query.Lookup(current.Id);
                    return files == null
                        ? Optional<NewerFilesOnModPage>.None
                           : new NewerFilesOnModPage(files.Value);
                }
            );
    }
}

/// <summary>
/// Marks all of the files on a mod page that are newer than the current ones.
/// </summary>
/// <param name="Files">
/// All the files on the mod page that are newer, these are sorted by uploaded date, descending,
/// meaning that the first file in the array is the newest.
/// </param>
public readonly record struct NewerFilesOnModPage(NexusModsFileMetadata.ReadOnly[] Files)
{
    /// <summary>
    /// Returns the newest file on the mod page.
    /// </summary>
    /// <remarks>
    /// Note(sewer): We by definition don't create this struct with empty arrays,
    /// so the first element is always present. 
    /// </remarks>
    public NexusModsFileMetadata.ReadOnly NewestFile() => Files[0];
}
