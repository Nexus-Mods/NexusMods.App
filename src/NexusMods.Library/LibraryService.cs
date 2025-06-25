using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly IGarbageCollectorRunner _gcRunner;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
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

    public async Task<LibraryFile.New> AddLibraryFile(ITransaction transaction, AbsolutePath source)
    {
        return await AddLibraryFileJob.Create(_serviceProvider, transaction, filePath: source);
    }

    public IJobTask<IInstallLoadoutItemJob, InstallLoadoutItemJobResult> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? itemInstaller = null,
        ILibraryItemInstaller? fallbackInstaller = null,
        ITransaction? transaction = null)
    {
        return InstallLoadoutItemJob.Create(_serviceProvider, libraryItem, targetLoadout, parent, itemInstaller, fallbackInstaller, transaction);
    }
    public async Task RemoveLibraryItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsynchronously)
    {
        using var tx = _connection.BeginTransaction();
        var items = libraryItems.ToArray();
        RemoveLinkedItemsFromAllLoadouts(items, tx);

        foreach (var item in items)
            tx.Delete(item.Id, recursive: true);

        await tx.Commit();
        await _gcRunner.RunWithMode(gcRunMode);
    }

    public async Task RemoveLinkedItemFromLoadout(LibraryLinkedLoadoutItemId itemId)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLinkedItemFromLoadout(itemId, tx);
        await tx.Commit();
    }

    public async Task RemoveLinkedItemsFromLoadout(IEnumerable<LibraryLinkedLoadoutItemId> itemIds)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLinkedItemsFromLoadout(itemIds, tx);
        await tx.Commit();
    }

    public void RemoveLinkedItemFromLoadout(LibraryLinkedLoadoutItemId itemId, ITransaction tx)
        => tx.Delete(itemId, recursive: true);

    public void RemoveLinkedItemsFromLoadout(IEnumerable<LibraryLinkedLoadoutItemId> itemIds, ITransaction tx)
    {
        foreach (var itemId in itemIds)
            tx.Delete(itemId, recursive: true);
    }

    public void RemoveLinkedItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems, ITransaction tx)
    {
        foreach (var item in libraryItems)
        {
            foreach (var (_, loadoutItem) in LoadoutsWithLibraryItem(item))
            {
                tx.Delete(loadoutItem.Id, recursive: true);
            }
        }
    }

    public async Task RemoveLinkedItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLinkedItemsFromAllLoadouts(libraryItems, tx);
        await tx.Commit();
    }
    public async ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem, ReplaceLibraryItemOptions options, ITransaction tx)
    {
        try
        {
            // 1. Find affected loadouts using existing method
            var items = LoadoutsWithLibraryItem(oldItem)
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
                .ToArray();

            // 2. Unlink old mod using bulk removal
            foreach (var (_, libraryLinkedItem) in items)
                RemoveLinkedItemFromLoadout(libraryLinkedItem.Id, tx);

            // 3. Reinstall new mod in original loadouts
            foreach (var (loadout, _) in items)
                await InstallItem(libraryItem: newItem, targetLoadout: loadout.Id, transaction: tx);
        }
        catch
        {
            return LibraryItemReplacementResult.FailedUnknownReason;
        }

        return LibraryItemReplacementResult.Success;
    }
    
    public async ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options, ITransaction tx)
    {
        var replacementsArr = replacements.ToArray();
        try
        {
            foreach (var (oldItem, newItem) in replacementsArr)
            {
                var result = await ReplaceLinkedItemsInAllLoadouts(oldItem, newItem, options.ToReplaceLibraryItemOptions(), tx);
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
    
    public async ValueTask<LibraryItemReplacementResult> ReplaceLinkedItemsInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements, ReplaceLibraryItemsOptions options)
    {
        using var tx = _connection.BeginTransaction();
        var result = await ReplaceLinkedItemsInAllLoadouts(replacements, options, tx);
        await tx.Commit();
        return result;
    }
}
