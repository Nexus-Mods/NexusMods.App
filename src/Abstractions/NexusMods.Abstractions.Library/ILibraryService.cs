using DynamicData;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Library;

/// <summary>
/// Represents the library.
/// </summary>
[PublicAPI]
public interface ILibraryService
{
    /// <summary>
    /// Adds a download to the library.
    /// </summary>
    [MustDisposeResource] IJob AddDownload(IDownloadJob downloadJob);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    [MustDisposeResource] IJob AddLocalFile(AbsolutePath absolutePath);

    /// <summary>
    /// Installs a library item into a target loadout.
    /// </summary>
    /// <param name="libraryItem">The item to install.</param>
    /// <param name="targetLoadout">The target loadout.</param>
    /// <param name="installer">The Library will use this installer to install the item</param>
    IJob InstallItem(LibraryItem.ReadOnly libraryItem, Loadout.ReadOnly targetLoadout, ILibraryItemInstaller? installer = null);

    IObservable<IChangeSet<LibraryFile.ReadOnly>> ObserveFilteredLibraryFiles();
}
