using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Extensions;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Library;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILoadoutManager _loadoutManager;
    private readonly IConnection _connection;
    private readonly IGarbageCollectorRunner _gcRunner;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _loadoutManager = serviceProvider.GetRequiredService<ILoadoutManager>();
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _gcRunner = serviceProvider.GetRequiredService<IGarbageCollectorRunner>();
    }

    public IJobTask<IAddDownloadJob, LibraryFile.ReadOnly> AddDownload(IJobTask<IDownloadJob, AbsolutePath> downloadJob)
    {
        return AddDownloadJob.Create(_serviceProvider, downloadJob);
    }

    public IJobTask<IAddLocalFile, LocalFile.ReadOnly> AddLocalFile(AbsolutePath absolutePath)
    {
        return AddLocalFileJob.Create(_serviceProvider, absolutePath);
    }

    public IEnumerable<(Loadout.ReadOnly loadout, LibraryLinkedLoadoutItem.ReadOnly linkedItem)> LoadoutsWithLibraryItem(LibraryItem.ReadOnly libraryItem)
    {
        var dbToUse = _connection.Db;
        // Start with a backref.
        // We're making a small assumption here that number of loadouts will be fairly small.
        // That may not always be true, but I believe
        return LibraryLinkedLoadoutItem
            .FindByLibraryItem(dbToUse, libraryItem)
            .Select(x => (x.AsLoadoutItem().Loadout, x));
    }

    public IReadOnlyDictionary<Loadout.ReadOnly, IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>> LoadoutsWithLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems)
    {
        var dbToUse = _connection.Db;
        var result = new Dictionary<Loadout.ReadOnly, List<(LibraryItem.ReadOnly, LibraryLinkedLoadoutItem.ReadOnly)>>();

        foreach (var libraryItem in libraryItems)
        {
            var linkedItems = LibraryLinkedLoadoutItem
                .FindByLibraryItem(dbToUse, libraryItem)
                .ToArray();
                
            foreach (var linkedItem in linkedItems)
            {
                var loadout = linkedItem.AsLoadoutItem().Loadout;
                if (!result.TryGetValue(loadout, out var list))
                {
                    list = new List<(LibraryItem.ReadOnly, LibraryLinkedLoadoutItem.ReadOnly)>();
                    result[loadout] = list;
                }
                list.Add((libraryItem, linkedItem));
            }
        }

        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>)kvp.Value.AsReadOnly()
        );
    }

    public IEnumerable<(CollectionGroup.ReadOnly collection, LibraryLinkedLoadoutItem.ReadOnly linkedItem)> CollectionsWithLibraryItem(LibraryItem.ReadOnly libraryItem, bool excludeReadOnlyCollections = false)
    {
        var dbToUse = _connection.Db;

        // Find all linked loadout items for this library item
        var linkedItems = LibraryLinkedLoadoutItem.FindByLibraryItem(dbToUse, libraryItem);

        foreach (var linkedItem in linkedItems)
        {
            // Walk up the parent chain to find the nearest collection group
            foreach (var parent in linkedItem.AsLoadoutItem().GetThisAndParents())
            {
                // First check if it's a LoadoutItemGroup, then if it's a CollectionGroup
                if (parent.TryGetAsLoadoutItemGroup(out var itemGroup) &&
                    itemGroup.TryGetAsCollectionGroup(out var collectionGroup))
                {
                    // Filter out read-only collections if requested
                    if (excludeReadOnlyCollections && collectionGroup.IsReadOnly)
                        break; // Skip this collection but continue looking up the parent chain
                    
                    yield return (collectionGroup, linkedItem);
                    break; // Found the nearest collection, no need to go further up
                }
            }
        }
    }

    public IReadOnlyDictionary<CollectionGroup.ReadOnly, IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>> CollectionsWithLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, bool excludeReadOnlyCollections = false)
    {
        var dbToUse = _connection.Db;
        var result = new Dictionary<CollectionGroup.ReadOnly, List<(LibraryItem.ReadOnly, LibraryLinkedLoadoutItem.ReadOnly)>>();
        
        foreach (var libraryItem in libraryItems)
        {
            var linkedItems = LibraryLinkedLoadoutItem
                .FindByLibraryItem(dbToUse, libraryItem)
                .ToArray();
                
            foreach (var linkedItem in linkedItems)
            {
                // Walk up the parent chain to find the nearest collection group
                foreach (var parent in linkedItem.AsLoadoutItem().GetThisAndParents())
                {
                    // First check if it's a LoadoutItemGroup, then if it's a CollectionGroup
                    if (parent.TryGetAsLoadoutItemGroup(out var itemGroup) && 
                        itemGroup.TryGetAsCollectionGroup(out var collectionGroup))
                    {
                        // Filter out read-only collections if requested
                        if (excludeReadOnlyCollections && collectionGroup.IsReadOnly)
                            break; // Skip this collection but continue looking up the parent chain
                        
                        if (!result.TryGetValue(collectionGroup, out var list))
                        {
                            list = new List<(LibraryItem.ReadOnly, LibraryLinkedLoadoutItem.ReadOnly)>();
                            result[collectionGroup] = list;
                        }
                        list.Add((libraryItem, linkedItem));
                        break; // Found the nearest collection, no need to go further up
                    }
                }
            }
        }
        
        return result.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<(LibraryItem.ReadOnly libraryItem, LibraryLinkedLoadoutItem.ReadOnly linkedItem)>)kvp.Value.AsReadOnly()
        );
    }

    public async Task<LibraryFile.New> AddLibraryFile(ITransaction transaction, AbsolutePath source)
    {
        return await AddLibraryFileJob.Create(_serviceProvider, transaction, filePath: source);
    }

    public async Task RemoveLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously)
    {
        var items = libraryItems.ToArray();
        var groupIds = items.SelectMany(LoadoutsWithLibraryItem).Select(tuple => tuple.linkedItem.AsLoadoutItemGroup().LoadoutItemGroupId).ToArray();
        await _loadoutManager.RemoveItems(groupIds);

        using var tx = _connection.BeginTransaction();

        foreach (var item in items)
            tx.Delete(item.Id, recursive: true);

        await tx.Commit();
        await _gcRunner.RunWithMode(gcRunMode);
    }

    public async ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem, ReplaceLibraryItemOptions options)
    {
        try
        {
            // 1. Find affected loadouts using existing method
            var itemsPerLoadout = LoadoutsWithLibraryItem(oldItem)
                .Where(tuple =>
                {
                    if (options.IgnoreReadOnlyCollections)
                    {
                        var collection = tuple.linkedItem.AsLoadoutItem().Parent;
                        var asCollectionGroup = collection.ToCollectionGroup();
                        return !asCollectionGroup.IsReadOnly;
                    }

                    return true;
                })
                .GroupBy(tuple => tuple.loadout.LoadoutId)
                .ToDictionary(grouping => grouping.Key, grouping => grouping.ToArray());

            foreach (var kv in itemsPerLoadout)
            {
                var (loadoutId, items) = kv;
                var groupIds = items.Select(x => x.linkedItem.AsLoadoutItemGroup().LoadoutItemGroupId).ToArray();
                await _loadoutManager.ReplaceItems(loadoutId, groupIds, newItem);
            }
        }
        catch
        {
            return LibraryItemReplacementResult.FailedUnknownReason;
        }

        return LibraryItemReplacementResult.Success;
    }
    
    public async ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options)
    {
        var replacementsArr = replacements.ToArray();
        try
        {
            foreach (var (oldItem, newItem) in replacementsArr)
            {
                var result = await ReplaceLinkedItemsInAllLoadouts(oldItem, newItem, options.ToReplaceLibraryItemOptions());
                if (result != LibraryItemReplacementResult.Success)
                    return result; // failed due to some reason.
            }
        }
        catch
        {
            return LibraryItemReplacementResult.FailedUnknownReason;
        }

        return LibraryItemReplacementResult.Success;
    }
}
