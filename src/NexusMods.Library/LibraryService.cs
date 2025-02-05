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
    private readonly IJobMonitor _monitor;
    private readonly IGarbageCollectorRunner _gcRunner;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _connection = serviceProvider.GetRequiredService<IConnection>();
        _monitor = serviceProvider.GetRequiredService<IJobMonitor>();
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

    public IJobTask<IInstallLoadoutItemJob, LoadoutItemGroup.ReadOnly> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? itemInstaller = null,
        ILibraryItemInstaller? fallbackInstaller = null)
    {
        if (!parent.HasValue)
        {
            if (!Loadout.Load(libraryItem.Db, targetLoadout).MutableCollections().TryGetFirst(out var userCollection))
                throw new InvalidOperationException("Could not find the user collection for the target loadout");
            parent = userCollection.AsLoadoutItemGroup().LoadoutItemGroupId;
        }

        return InstallLoadoutItemJob.Create(_serviceProvider, libraryItem, parent.Value, itemInstaller, fallbackInstaller);
    }
    public async Task RemoveItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsyncInBackground)
    {
        using var tx = _connection.BeginTransaction();
        foreach (var item in libraryItems)
            tx.Delete(item.Id, true);

        await tx.Commit();
        _gcRunner.RunWithMode(gcRunMode);
    }
}
