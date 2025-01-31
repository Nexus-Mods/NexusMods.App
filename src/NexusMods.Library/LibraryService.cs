using DynamicData.Kernel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Paths;

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

    public IEnumerable<Loadout.ReadOnly> LoadoutsWithLibraryItem(LibraryItem.ReadOnly libraryItem, IDb? db = null)
    {
        var dbToUse = db ?? libraryItem.Db;
        // Start with a backref.
        // We're making a small assumption here that number of loadouts will be fairly small.
        // That may not always be true, but I believe
        return LibraryLinkedLoadoutItem
            .FindByLibraryItem(dbToUse, libraryItem)
            .Select(x => x.AsLoadoutItem().Loadout);
    }

    public IJobTask<IInstallLoadoutItemJob, LoadoutItemGroup.ReadOnly> InstallItem(LibraryItem.ReadOnly libraryItem, LoadoutId targetLoadout, Optional<LoadoutItemGroupId> parent = default, ILibraryItemInstaller? itemInstaller = null, ITransaction? transaction = null)
    {
        if (!parent.HasValue)
        {
            if (!Loadout.Load(libraryItem.Db, targetLoadout).MutableCollections().TryGetFirst(out var userCollection))
                throw new InvalidOperationException("Could not find the user collection for the target loadout");
            parent = userCollection.AsLoadoutItemGroup().LoadoutItemGroupId;
        }

        return InstallLoadoutItemJob.Create(_serviceProvider, libraryItem, parent.Value, itemInstaller);
    }

    public async Task RemoveItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsyncInBackground)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLibraryItemsFromAllLoadouts(libraryItems, tx);

        foreach (var item in libraryItems)
            tx.Delete(item.Id, recursive: true);

        await tx.Commit();
        _gcRunner.RunWithMode(gcRunMode);
    }

    public async Task RemoveLibraryItemFromLoadout(LoadoutItemId itemId)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLibraryItemFromLoadout(itemId, tx);
        await tx.Commit();
    }

    public async Task RemoveLibraryItemFromLoadout(IEnumerable<LoadoutItemId> itemIds)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLibraryItemFromLoadout(itemIds, tx);
        await tx.Commit();
    }

    public void RemoveLibraryItemFromLoadout(LoadoutItemId itemId, ITransaction tx)
        => RemoveLibraryItemFromLoadout([itemId], tx);

    public void RemoveLibraryItemFromLoadout(IEnumerable<LoadoutItemId> itemIds, ITransaction tx)
    {
        foreach (var itemId in itemIds)
            tx.Delete(itemId, recursive: true);
    }

    public void RemoveLibraryItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems, ITransaction tx)
    {
        foreach (var item in libraryItems)
        {
            foreach (var loadout in LoadoutsWithLibraryItem(item))
            {
                foreach (var loadoutItem in loadout.GetLoadoutItemsByLibraryItem(item))
                {
                    tx.Delete(loadoutItem.Id, recursive: true);
                }
            }
        }
    }

    public async Task RemoveLibraryItemsFromAllLoadouts(IEnumerable<LibraryItem.ReadOnly> libraryItems)
    {
        using var tx = _connection.BeginTransaction();
        RemoveLibraryItemsFromAllLoadouts(libraryItems, tx);
        await tx.Commit();
    }

    public async ValueTask<LibraryItemReplacementResult> ReplaceLibraryItemInAllLoadouts(LibraryItem.ReadOnly oldMod, LibraryItem.ReadOnly newMod, ITransaction tx)
    {
        try
        {
            // 1. Find affected loadouts using existing method
            var loadouts = LoadoutsWithLibraryItem(oldMod)
                .Select(l => l.Id)
                .ToArray();

            // 2. Unlink old mod using bulk removal
            foreach (var item in loadouts)
                RemoveLibraryItemFromLoadout(item, tx);

            // 3. Reinstall new mod in original loadouts
            foreach (var loadoutId in loadouts)
                await InstallItem(libraryItem: newMod, targetLoadout: loadoutId);

            // TODO(sewer): This does not handle collections properly, which we don't yet have a 
            // strategy for as far as their read only nature is concerned. We ideally should
            // not allow the user to start the update operation on collection items in the first place,
            // they should be considered as 'managed by the collection'.
        }
        catch
        {
            return LibraryItemReplacementResult.FailedUnknownReason;
        }

        return LibraryItemReplacementResult.Success;
    }

    public async ValueTask<LibraryItemReplacementResult> ReplaceLibraryItemInAllLoadouts(IEnumerable<(LibraryItem.ReadOnly oldItem, LibraryItem.ReadOnly newItem)> replacements)
    {
        using var tx = _connection.BeginTransaction();
        try
        {
            foreach (var (oldItem, newItem) in replacements)
            {
                var result = await ReplaceLibraryItemInAllLoadouts(oldItem, newItem, tx);
                if (result != LibraryItemReplacementResult.Success)
                    return result; // failed due to some reason.
            }
            await tx.Commit();
        }
        catch
        {
            return LibraryItemReplacementResult.FailedUnknownReason;
        }

        // 4. Clean up old mod using existing garbage collection
        await RemoveItems(replacements.Select(r => r.oldItem), GarbageCollectorRunMode.RunAsyncInBackground);
        return LibraryItemReplacementResult.Success;
    }
}
