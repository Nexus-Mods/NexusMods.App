using System.Reactive.Linq;
using System.Reactive.Subjects;
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
    private readonly Subject<KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>> _newestModVersionSubject = new();
    private readonly Subject<KeyValuePair<NexusModsModPageMetadata.ReadOnly, NewestModPageVersionData>> _newestModOnAnyPageSubject = new();

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
        
        // Start a transaction with updated info if at least 1 item needs
        // updating with upstream server
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
        foreach (var metadata in NexusModsFileMetadata.All(_connection.Db))
        {
            var newerItems = RunUpdateCheck.GetNewerFilesForExistingFile(metadata);
            var mostRecentItem = newerItems.FirstOrDefault();
            if (!mostRecentItem.IsValid()) // Catch case of no newer items.
                continue;

            // Notify the file of its update.                                                                                                                                                                                                         
            _newestModVersionSubject.OnNext(new KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>(metadata, mostRecentItem)); 
        }
        
        // Check every mod page, and notify it of its update.
        foreach (var modPage in NexusModsModPageMetadata.All(_connection.Db))
        {
            var newestDate = DateTimeOffset.MinValue;
            NexusModsFileMetadata.ReadOnly newestItem = default;
            var numToUpdate = 0;
            var isAnyOnModPageNewer = false;

            // Check all mods within the mod page; finding the newest one.
            foreach (var modFile in modPage.Files)
            {
                var newerItems = RunUpdateCheck.GetNewerFilesForExistingFile(modFile);
                var mostRecentItem = newerItems.FirstOrDefault();
                if (!mostRecentItem.IsValid()) // Catch case of no newer items.
                    continue;

                var isNewestOnModPage = mostRecentItem.UploadedAt > newestDate;
                if (!isNewestOnModPage)
                    continue;

                numToUpdate++;
                newestDate = mostRecentItem.UploadedAt;
                newestItem = mostRecentItem;
                isAnyOnModPageNewer = true;
            }

            if (isAnyOnModPageNewer)
                _newestModOnAnyPageSubject.OnNext(new KeyValuePair<NexusModsModPageMetadata.ReadOnly, NewestModPageVersionData>(modPage, new NewestModPageVersionData(newestItem, numToUpdate)));
        }
    }

    /// <inheritdoc />
    public IObservable<NexusModsFileMetadata.ReadOnly> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current)
    {
        return _newestModVersionSubject
            .Where(kv => kv.Key.Id == current.Id) // fastest possible compare
            .Select(kv => kv.Value); 
        // Note(sewer): Value is valid by definition, we only beam valid values
    }
    
    /// <inheritdoc />
    public IObservable<NewestModPageVersionData> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current)
    {
        return _newestModOnAnyPageSubject
            .Where(kv => kv.Key.Id == current.Id) // fastest possible compare
            .Select(kv => kv.Value); 
        // Note(sewer): Value is valid by definition, we only beam valid values
    }
}
