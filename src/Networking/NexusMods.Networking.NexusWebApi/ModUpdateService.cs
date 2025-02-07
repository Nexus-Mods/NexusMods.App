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
    private readonly SourceCache<KeyValuePair<NexusModsFileMetadataId, ModUpdateOnPage>, EntityId> _newestModVersionCache = new (static kv => kv.Key);
    private readonly SourceCache<KeyValuePair<NexusModsModPageMetadataId, ModUpdatesOnModPage>, EntityId> _newestModOnAnyPageCache = new (static kv => kv.Key);

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
                    // `!filesInLibrary.ContainsKey(newFile.Id)`: This filters out the case where we already have the latest file version on disk. 
                    .Where(newFile => newFile.IsValid() && !filesInLibrary.ContainsKey(newFile.Id))
                    .ToArray();

                return new ModUpdateOnPage(kv.Value, newerFiles);
            })
            .Where(static kv => kv.NewerFiles.Length > 0)
            .ToArray();

        foreach (var kv in existingFileToNewerFiles)
            _newestModVersionCache.AddOrUpdate(new KeyValuePair<NexusModsFileMetadataId, ModUpdateOnPage>(kv.File.Id, kv));

        var modPageToNewerFiles = existingFileToNewerFiles
            .GroupBy(
                kv => kv.File.ModPageId,
                kv => kv
            )
            .ToDictionary(
                group => group.Key,
                group => (ModUpdatesOnModPage)group.ToArray()
            );

        foreach (var kv in modPageToNewerFiles)
            _newestModOnAnyPageCache.AddOrUpdate(kv);
    }

    /// <inheritdoc />
    public IObservable<Optional<ModUpdateOnPage>> GetNewestFileVersionObservable(NexusModsFileMetadata.ReadOnly current)
    {
        return _newestModVersionCache.Connect()
            .Transform(kv => kv.Value)
            .QueryWhenChanged(query => query.Lookup(current.Id));
    }

    /// <inheritdoc />
    public IObservable<Optional<ModUpdatesOnModPage>> GetNewestModPageVersionObservable(NexusModsModPageMetadata.ReadOnly current)
    {
        return _newestModOnAnyPageCache.Connect()
            .Transform(kv => kv.Value)
            .QueryWhenChanged(query => query.Lookup(current.Id));
    }
}

/// <summary>
/// Marks all the files on a mod page that are newer than the current ones.
/// </summary>
/// <param name="FileMappings">
/// An array, where each entry is a mapping of a single file (currently in the library) to its newer versions
/// available on `nexusmods.com`.
/// </param>
public readonly record struct ModUpdatesOnModPage(ModUpdateOnPage[] FileMappings)
{
    /// <summary>
    /// This returns the number of mod files to update on this page.
    /// Given that each array entry represents a single mod file, this is just the count of the internal array.
    /// </summary>
    public int NumberOfModFilesToUpdate => FileMappings.Length;
    
    /// <summary>
    /// Returns the newest file across mods on this mod page.
    /// </summary>
    public NexusModsFileMetadata.ReadOnly NewestFile()
    {
        // Note(sewer): This matches the behaviour established in the design for
        // the mod update feature. The row should show the details of the newest mod.
        // In our case, we simply need to select the most recent file across all mods
        // within this page. In practice, there's usually only one mod, but there can
        // sometimes be more in some rare cases.
        
        // Compare the newest file in all `FileMappings` and return most recent one
        // (without LINQ, avoid alloc, since every mod row will touch this code in UI).
        var newestFile = FileMappings[0].NewerFiles[0];
        for (var x = 1; x < FileMappings.Length; x++)
        {
            var newerFile = FileMappings[x].NewerFiles[0];
            if (newerFile.UploadedAt > newestFile.UploadedAt)
                newestFile = newerFile;
        }
        
        return newestFile;
    }
    
    /// <summary>
    /// Returns the newest file from every mod on this page
    /// </summary>
    public IEnumerable<NexusModsFileMetadata.ReadOnly> NewestFileForEachPage() => FileMappings.Select(x => x.NewestFile);

    /// <summary/>
    public static explicit operator ModUpdatesOnModPage(ModUpdateOnPage[] inner) => new(inner);
    
    /// <summary/>
    public static explicit operator ModUpdatesOnModPage(ModUpdateOnPage inner) => new([inner]); // promote single item to array.
}

/// <summary>
/// Represents a mapping of a single file (currently in the library) to its newer versions
/// available on `nexusmods.com`.
/// </summary>
/// <param name="File">Unique identifier for the file that's currently in the library.</param>
/// <param name="NewerFiles">Newer files for this specific library file.</param>
public readonly record struct ModUpdateOnPage(NexusModsFileMetadata.ReadOnly File, NexusModsFileMetadata.ReadOnly[] NewerFiles)
{
    /// <summary>
    /// The newest update file for this individual mod on a mod page.
    /// </summary>
    public NexusModsFileMetadata.ReadOnly NewestFile => NewerFiles[0];
    
    /// <summary/>
    public static implicit operator ModUpdateOnPage(KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly[]> kv) => new(kv.Key, kv.Value);
    /// <summary/>
    public static implicit operator KeyValuePair<NexusModsFileMetadata.ReadOnly, NexusModsFileMetadata.ReadOnly[]>(ModUpdateOnPage modUpdateOnPage) => new(modUpdateOnPage.File, modUpdateOnPage.NewerFiles);
}
