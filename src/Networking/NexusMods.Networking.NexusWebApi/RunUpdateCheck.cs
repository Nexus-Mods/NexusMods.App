using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;
using NexusMods.Networking.NexusWebApi.Extensions;
using StrawberryShake;
namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Utility class that encapsulates the logic for running the actual update check.
/// </summary>
public static class RunUpdateCheck
{
    /// <summary>
    /// Identifies all mod pages whose information needs refreshed.
    /// </summary>
    public static async Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckForModPagesWhichNeedUpdating(IDb db, INexusApiClient apiClient)
    {
        // Extract all GameDomain(s)
        var modPages = PageMetadataMixin.EnumerateDatabaseEntries(db).ToArray();
        var gameIds = modPages.Select(x => (x.GetModPageId().GameId)).Distinct().ToArray();
        
        // Note: The v1Timespan accounts for 1 month minus 5 minutes
        //  - We use 28 days because February is the shortest month at 28.
        //  - Serverside caches for 5 minutes, so we subtract that. 
        var v1Timespan = TimeSpan.FromDays(28).Subtract(TimeSpan.FromMinutes(5));
        var updater = new MultiFeedCacheUpdater<PageMetadataMixin>(modPages, v1Timespan);

        foreach (var gameId in gameIds)
        {
            // Note (sewer): We need to update to V2 stat.
            var modUpdates = await apiClient.ModUpdatesAsync(gameId.ToGameDomain().Value, PastTime.Month);
            var updateResults = ModFeedItemUpdateMixin.FromUpdateResults(modUpdates.Data, gameId);
            updater.Update(updateResults);
        }
        
        return updater.BuildFlattened();
    }

    /// <summary>
    /// Updates the metadata for mod pages returned from the <see cref="CheckForModPagesWhichNeedUpdating"/> API call.
    /// </summary>
    public static async Task UpdateModFilesForOutdatedPages(IDb db, ITransaction tx, ILogger logger, INexusGraphQLClient gqlClient, PerFeedCacheUpdaterResult<PageMetadataMixin> result, CancellationToken cancellationToken)
    {
        // Note(sewer): Undetermined items may be removed items from the site; or
        // caused by programmer error, so wr should log these whenever possible,
        // but they should not cause a critical error; in case it's simply the result
        // of mod removal such as DMCA takedown.
        foreach (var mixin in result.UndeterminedItems)
        {
            try
            {
                await UpdateModPage(db, tx, gqlClient, cancellationToken, mixin);
            }
            catch (Exception e)
            {
                var id = mixin.GetModPageId();
                logger.LogError(e, "Failed to update metadata for Mod (GameID: {Page}, ModId: {ModId})", id.GameId, id.ModId);
            }
        }

        // Note(sewer): But I'm not sure where to put this yet, all the GraphQL stuff is source generated.
        foreach (var mixin in result.OutOfDateItems)
        {
            // For the remaining items, failure to obtain result here should be truly exceptional.
            await UpdateModPage(db, tx, gqlClient, cancellationToken, mixin);
        }
    }

    private static async Task UpdateModPage(IDb db, ITransaction tx, INexusGraphQLClient gqlClient, CancellationToken cancellationToken, PageMetadataMixin mixin)
    {
        var uid = mixin.GetModPageId();
        var modIdString = uid.ModId.Value.ToString();
        var gameIdString = uid.GameId.Value.ToString();
        
        // Update Mod
        var modInfo = await gqlClient.ModInfo.ExecuteAsync((int)uid.GameId.Value, (int)uid.ModId.Value, cancellationToken);
        foreach (var node in modInfo.Data!.LegacyMods.Nodes)
            node.Resolve(db, tx);
        
        // Update Mod Files
        var filesByUid = await gqlClient.ModFiles.ExecuteAsync(modIdString, gameIdString, cancellationToken);
        filesByUid.EnsureNoErrors();

        var pageEntityId = mixin.GetModPageEntityId();
        foreach (var node in filesByUid.Data!.ModFiles)
            node.Resolve(db, tx, pageEntityId);
    }

    /// <summary>
    /// Returns all files which have a 'newer' date than the current one.
    /// </summary>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetNewerFilesForExistingFile(IDb db, UidForFile uid)
    {
        var metadata = NexusModsFileMetadata.FindByUid(db, uid).First();
        return GetNewerFilesForExistingFile(metadata);
    } 
    
    /// <summary>
    /// Returns all files which have a 'newer' date than the current one.
    /// </summary>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetNewerFilesForExistingFile(NexusModsFileMetadata.ReadOnly file)
    {
        return file.ModPage.Files.Where(x => x.UploadedAt > file.UploadedAt);
    } 
}
