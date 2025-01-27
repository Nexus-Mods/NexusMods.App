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
    private readonly Subject<KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>> _newestVersionSubject = new();   

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

    public async Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckAndUpdateMods(CancellationToken token)
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
        
        // Notify every file of its update.
        foreach (var metadata in NexusModsFileMetadata.All(_connection.Db))
        {
            var newerItems = RunUpdateCheck.GetNewerFilesForExistingFile(metadata);
            var mostRecentItem = newerItems.FirstOrDefault();
            if (!mostRecentItem.IsValid()) // Catch case of no newer items.
                continue;

            // Notify the file of its update.                                                                                                                                                                                                         
            _newestVersionSubject.OnNext(new KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly>(metadata, mostRecentItem)); 
        }

        return updateCheckResult;
    }

    /// <inheritdoc />
    public IObservable<NexusModsFileMetadata.ReadOnly> GetNewestVersionObservable(NexusModsFileMetadata.ReadOnly current)
    {
        return _newestVersionSubject
            .Where(kv => kv.Key.Id == current.Id) // fastest possible compare
            .Select(kv => kv.Value); 
        // Note(sewer): Value is valid by definition, we only beam valid values
    }
}
