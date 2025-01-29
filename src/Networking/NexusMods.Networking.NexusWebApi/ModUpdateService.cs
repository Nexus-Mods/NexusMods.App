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
    private readonly SourceCache<KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>, EntityId> _newestModVersionCache = 
        new (x => x.Key.Id);
    private readonly SourceCache<KeyValuePair<NexusModsModPageMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly[]>, EntityId> _newestModOnAnyPageCache
        = new(x => x.Key.Id);

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
        // Notify every file of its update.
        var allLibraryItems = NexusModsLibraryItem.All(_connection.Db).ToDictionary(x => x.FileMetadata.Id);
        foreach (var libraryItem in allLibraryItems.Values)
        {
            var metadata = libraryItem.FileMetadata;
            var newerItems = RunUpdateCheck.GetNewerFilesForExistingFile(metadata);
            var mostRecentItem = newerItems.FirstOrDefault();
            if (!mostRecentItem.IsValid() || allLibraryItems.ContainsKey(mostRecentItem.Id)) continue;

            // Notify the file of its update.                                                                                                                                                                                                         
            var kvp = new KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>(metadata, mostRecentItem);
            _newestModVersionCache.AddOrUpdate(kvp);
        }

        // Check every mod page, and notify it of its update.
        foreach (var modPage in NexusModsModPageMetadata.All(_connection.Db))
        {
            var files = new List<NexusModsFileMetadata.ReadOnly>();

            foreach (var modFile in modPage.Files.Where(x => allLibraryItems.ContainsKey(x.Id)))
            {
                var newerItems = RunUpdateCheck.GetNewerFilesForExistingFile(modFile);
                files.AddRange(newerItems.Where(x => !allLibraryItems.ContainsKey(x)));
            }

            if (files.Count == 0) continue;
            files.Sort(static (a, b) => a.UploadedAt.CompareTo(b.UploadedAt));

            var kvp = new KeyValuePair<NexusModsModPageMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly[]>(modPage, files.ToArray());
            _newestModOnAnyPageCache.AddOrUpdate(kvp);
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
    public IObservable<Optional<NexusModsFileMetadata.ReadOnly[]>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current)
    {
        return _newestModOnAnyPageCache.Connect()
            .Transform(kv => kv.Value)
            .QueryWhenChanged(query => query.Lookup(current.Id));
    }
}
