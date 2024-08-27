using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
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

    public IJob AddDownload(IDownloadJob downloadJob)
    {
        var job = new AddDownloadJob(monitor: _monitor, worker: _serviceProvider.GetRequiredService<AddDownloadJobWorker>())
        {
            DownloadJob = downloadJob,
        };

        return job;
    }

    public IJob AddLocalFile(AbsolutePath absolutePath)
    {
        var group = new AddLocalFileJob(monitor: _monitor, worker: _serviceProvider.GetRequiredService<AddLocalFileJobWorker>())
        {
            Transaction = _connection.BeginTransaction(),
            FilePath = absolutePath,
        };

        return group;
    }

    public IJob InstallItem(LibraryItem.ReadOnly libraryItem, LoadoutId targetLoadout, ILibraryItemInstaller? itemInstaller = null)
    {
        var loadout = Loadout.Load(_connection.Db, targetLoadout);
        var job = new InstallLoadoutItemJob(monitor: _monitor, worker: _serviceProvider.GetRequiredService<InstallLoadoutItemJobWorker>())
        {
            Connection = _connection,
            LibraryItem = libraryItem,
            Loadout = loadout,
            Installer = itemInstaller,
        };

        return job;
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
