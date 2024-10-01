using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates.Mixins;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.Extensions;
using StrawberryShake;
namespace NexusMods.Networking.ModUpdates;

/// <summary>
/// Utility class that encapsulates the logic for running the actual update check.
/// </summary>
public class RunUpdateCheck
{
    /// <summary>
    /// Identifies all mod pages whose information needs refreshed.
    /// </summary>
    public async Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckForModPagesWhichNeedUpdating(IDb db, INexusApiClient apiClient)
    {
        // Extract all GameDomain(s)
        var modPages = PageMetadataMixin.EnumerateDatabaseEntries(db).ToArray();
        var gameIds = modPages.Select(x => (x.GetUniqueId().GameId)).Distinct().ToArray();
        
        // Note: The v1Timespan accounts for 1 month minus 5 minutes
        //  - We use 28 days because February is the shortest month at 28.
        //  - Serverside caches for 5 minutes, so we subtract that. 
        var v1Timespan = TimeSpan.FromDays(28).Subtract(TimeSpan.FromMinutes(5));
        var updater = new MultiFeedCacheUpdater<PageMetadataMixin>(modPages, v1Timespan);

        foreach (var gameId in gameIds)
        {
            // Note (sewer): This is messy, we need to update to V2 stat.
            var modUpdates = await apiClient.ModUpdatesAsync(gameId.ToGameDomain().Value, PastTime.Month);
            var updateResults = ModUpdateMixin.FromUpdateResults(modUpdates.Data, gameId);
            updater.Update(updateResults);
        }
        
        return updater.BuildFlattened();
    }

    /// <summary>
    /// Updates the metadata for mod pages returned from the <see cref="CheckForModPagesWhichNeedUpdating"/> API call.
    /// </summary>
    public async Task UpdateModFilesForPage(IDb db, ITransaction tx, ILogger logger, INexusGraphQLClient gqlClient, PerFeedCacheUpdaterResult<PageMetadataMixin> result, CancellationToken cancellationToken)
    {
        // Undetermined items may be removed items from the site; these risk
        // causing errors, so we silently discard them while logging.
        // If we ever find we missed some edge case, logs will help.
        foreach (var mixin in result.UndeterminedItems)
        {
            try
            {
                // Fetch the items individually. 
                var uid = mixin.GetUniqueId().ToV2Api();
                var filesByUid = await gqlClient.ModFilesByUid.ExecuteAsync([uid], cancellationToken);
                filesByUid.EnsureNoErrors();
                
                // Update the metadata
                UpdateDatabase(filesByUid);
            }
            catch (Exception e)
            {
                var id = mixin.GetUniqueId();
                logger.LogError(e, "Failed toi update metadata for Mod (GameID: {Page}, ModId: {ModId})", id.GameId, id.ModId);
            }
        }

        // TODO: Move constant to a better location.
        // 50 is max number of items that API allows returned at once.
        // Note(sewer): But I'm not sure where to put this yet, all the GraphQL stuff is source generated.
        var groupsOfMaxItems = result.OutOfDateItems.Chunk(50);
        foreach (var itemGroup in groupsOfMaxItems)
        {
            var uids = itemGroup.Select(x => x.GetUniqueId().ToV2Api()).ToList();
            var filesByUid = await gqlClient.ModFilesByUid.ExecuteAsync(uids, cancellationToken);
            filesByUid.EnsureNoErrors();
            
            // Update the metadata
            UpdateDatabase(filesByUid);
        }
        return;

        void UpdateDatabase(IOperationResult<IModFilesByUidResult> filesByUid)
        {
            foreach (var node in filesByUid.Data!.ModFilesByUid!.Nodes)
                node.Resolve(db, tx);
        }
    }

    /// <summary>
    /// Returns all files which have a 'newer' date than the current one.
    /// </summary>
    public IEnumerable<NexusModsFileMetadata.ReadOnly> GetNewerFilesForExistingFile(NexusModsFileMetadata.ReadOnly file)
    {
        return file.ModPage.Files.Where(x => x.UploadedAt > file.UploadedAt);
    } 
}
