using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Library;

/// <summary>
/// Implementation of <see cref="ILibraryService"/>.
/// </summary>
public sealed class LibraryService : ILibraryService
{
    private readonly ILogger _logger;
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LibraryService(ILogger<LibraryService> logger, IConnection connection, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _connection = connection;
        _serviceProvider = serviceProvider;
    }

    public IJob AddDownload(IDownloadJob downloadJob)
    {
        throw new NotImplementedException();
    }

    public IJob AddLocalFile(AbsolutePath absolutePath)
    {
        var group = new AddLocalFileJob(worker: _serviceProvider.GetRequiredService<AddLocalFileJobWorker>())
        {
            Transaction = _connection.BeginTransaction(),
            FilePath = absolutePath,
        };

        return group;
    }

    public IJob InstallItem(LibraryItem.ReadOnly libraryItem, Loadout.ReadOnly targetLoadout, ILibraryItemInstaller? itemInstaller = null)
    {
        var job = new InstallLoadoutItemJob(worker: _serviceProvider.GetRequiredService<InstallLoadoutItemJobWorker>())
        {
            Connection = _connection,
            LibraryItem = libraryItem,
            Loadout = targetLoadout,
            Installer = itemInstaller,
        };

        return job;
    }
}
