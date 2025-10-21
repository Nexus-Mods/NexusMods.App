using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2.Uid;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.ModUpdates;
using NexusMods.Networking.ModUpdates.Mixins;
using NexusMods.Networking.ModUpdates.Traits;
using NexusMods.Networking.NexusWebApi.Extensions;
namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Utility class that encapsulates the logic for running the actual update check.
/// </summary>
public static class RunUpdateCheck
{
    /// <summary>
    /// Identifies all mod pages whose information needs refreshed.
    /// </summary>
    public static async Task<PerFeedCacheUpdaterResult<PageMetadataMixin>> CheckForModPagesWhichNeedUpdating(IDb db, INexusApiClient apiClient, IGameDomainToGameIdMappingCache mappingCache)
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
            var gameDomain = mappingCache[gameId].Value;
            var modUpdates = await apiClient.ModUpdatesAsync(gameDomain, PastTime.Month);
            var updateResults = ModFeedItemUpdateMixin.FromUpdateResults(modUpdates.Data, gameId);
            updater.Update(updateResults);
        }
        
        return updater.BuildFlattened(v1Timespan);
    }

    /// <summary>
    /// Updates the metadata for mod pages returned from the <see cref="CheckForModPagesWhichNeedUpdating"/> API call.
    /// </summary>
    public static async Task UpdateModFilesForOutdatedPages(
        IDb db,
        ITransaction tx,
        ILogger logger,
        IGraphQlClient graphQlClient,
        PerFeedCacheUpdaterResult<PageMetadataMixin> result,
        CancellationToken cancellationToken)
    {
        // Note(sewer): Undetermined items may be removed items from the site; or
        // caused by programmer error, so we should log these whenever possible,
        // but they should not cause a critical error; in case it's simply the result
        // of mod removal such as DMCA takedown.
        
        // Note(sewer): The semaphore below limits the maximum number of requests to the Nexus API
        // (UpdateModPage) that can be in transit at any time. i.e., We will process max
        // 'semaphoreCount' requests at a time. The number specified in the constructor
        // is the maximum number of requests that can be in transit at any given time.
        // It is completely arbitrary and can be safely changed. If the API ever adds 
        // rate limits, this may need tweaking.
        //
        // When this code is important is if the user doesn't use the App for a long time,
        // i.e. past the cache expiry time. Currently, that's 28 days. Past that point,
        // when checking for an update, all the mod pages will need refreshing; 
        // which will kick in this rate limiter. Otherwise, outside of that, concurrent
        // requests here are very unlikely.
        using var semaphore = new SemaphoreSlim(16);
        var tasks = new List<Task>();

        // Helper function to process a single mixin with error handling
        async Task ProcessMixin(SemaphoreSlim sema, PageMetadataMixin mixin, bool isUndetermined)
        {
            var isTaken = false;
            try
            {
                isTaken = await sema.WaitAsync(Timeout.Infinite, cancellationToken);
                await UpdateModPage(db, tx,
                    graphQlClient,
                    mixin,
                    cancellationToken
                );
            }
            catch (OperationCanceledException)
            {
                // Ignored.
            }
            catch (Exception e)
            {
                if (isUndetermined)
                {
                    var id = mixin.GetModPageId();
                    logger.LogError(e, "Failed to update metadata for Mod (GameID: {Page}, ModId: {ModId})", id.GameId, id.ModId);
                }
                else
                {
                    // Rethrow for non-undetermined items as these should be truly exceptional
                    throw;
                }
            }
            finally
            {
                if (isTaken)
                    sema.Release();
            }
        }

        // Process undetermined items
        foreach (var mixin in result.UndeterminedItems)
        {
            tasks.Add(ProcessMixin(semaphore, mixin, true));
        }

        // Process out of date items
        foreach (var mixin in result.OutOfDateItems)
        {
            tasks.Add(ProcessMixin(semaphore, mixin, false));
        }

        // Wait for all tasks to complete
        await Task.WhenAll(tasks);
    }

    private static async Task UpdateModPage(
        IDb db,
        ITransaction tx,
        IGraphQlClient graphQlClient,
        PageMetadataMixin mixin,
        CancellationToken cancellationToken = default)
    {
        var uid = mixin.GetModPageId();

        var modResult = await graphQlClient.QueryMod(uid.ModId, uid.GameId, cancellationToken);
        // TODO: handle errors
        var mod = modResult.AssertHasData();

        mod.Resolve(db, tx, setFilesTimestamp: true);

        // Update Mod Files
        var pageEntityId = mixin.GetModPageEntityId();

        var modFilesResult = await graphQlClient.QueryModFiles(uid.ModId, uid.GameId, cancellationToken: cancellationToken);
        // TODO: handle errors
        var modFiles = modFilesResult.AssertHasData();

        foreach (var modFile in modFiles)
        {
            modFile.Resolve(db, tx, pageEntityId);
        }
    }

    /// <summary>
    /// Returns all files which have a 'newer' date than the current one.
    /// </summary>
    /// <remarks>
    ///     The returned items are returned in descending order, from newest to oldest.
    /// </remarks>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetNewerFilesForExistingFile(IDb db, FileUid fileUid)
    {
        var metadata = NexusModsFileMetadata.FindByUid(db, fileUid).First();
        return GetNewerFilesForExistingFile(metadata);
    } 
    
    /// <summary>
    /// Returns all files which have a 'newer' date than the current one.
    /// </summary>
    /// <remarks>
    ///     The returned items are returned in descending order, from newest to oldest.
    /// </remarks>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetNewerFilesForExistingFile(NexusModsFileMetadata.ReadOnly file) 
        => GetNewerFilesForExistingFile(new ModFileMetadataMixin(file)).Select(x => ((ModFileMetadataMixin)x).Metadata);

    /// <summary>
    /// Returns all files which have a 'newer' date than the current one,
    /// using fuzzy name matching to handle variations in file naming.
    /// </summary>
    /// <remarks>
    ///     The returned items are returned in descending order, from newest to oldest.
    /// </remarks>
    public static IEnumerable<IAmAModFile> GetNewerFilesForExistingFile(IAmAModFile file)
    {
        // Get the normalized name for the current file
        var normalizedName = FuzzySearch.NormalizeFileName(file.Name, file.Version);
        
        // Get all other files from the same mod page
        return file.OtherFilesInSameModPage
            .Where(otherFile => 
                // Must be uploaded later
                // Note(sewer):
                //
                //     In future we might check version too, but this may
                //     be a bit unreliable in cases where versions have
                //     a suffix and non-suffix variants. So these items would
                //     need to be put into a different group/pool to match from.
                //
                //     Because, when mapped to SemVer, the suffix would be
                //     interpreted as a pre-release, and an item without a suffix
                //     may be assumed as a more recent version.
                otherFile.UploadedAt > file.UploadedAt &&
                // Must have matching normalized name
                FuzzySearch.NormalizeFileName(otherFile.Name, otherFile.Version)
                    .Equals(normalizedName, StringComparison.OrdinalIgnoreCase)
            )
            .OrderByDescending(x => x.UploadedAt);
    }

    /// <summary>
    /// Returns all file versions of a given file, including:
    /// - Current (itself) 
    /// - All older versions
    /// - All newer versions
    /// 
    /// Using fuzzy name matching.
    /// </summary>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetAllVersionsForExistingFile(NexusModsFileMetadata.ReadOnly file)
    {
        var mixin = new ModFileMetadataMixin(file);
        var normalizedName = FuzzySearch.NormalizeFileName(file.Name, file.Version);
        // Collect matching versions, include current file, then sort by upload date
        return mixin.OtherFilesInSameModPage
            .Where(f => FuzzySearch.NormalizeFileName(f.Name, f.Version)
                .Equals(normalizedName, StringComparison.OrdinalIgnoreCase))
            .Select(f => ((ModFileMetadataMixin)f).Metadata)
            .Append(file)
            .OrderBy(f => f.UploadedAt);
    }

    /// <summary>
    /// Returns all file versions for a file identified by UID; including itself.
    /// </summary>
    public static IEnumerable<NexusModsFileMetadata.ReadOnly> GetAllVersionsForExistingFile(IDb db, FileUid fileUid)
    {
        var metadata = NexusModsFileMetadata.FindByUid(db, fileUid).First();
        return GetAllVersionsForExistingFile(metadata);
    }
}
