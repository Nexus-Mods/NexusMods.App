using DynamicData.Kernel;
using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.GC;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Library.Jobs;
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
    IJobTask<IAddDownloadJob, LibraryFile.ReadOnly> AddDownload(IJobTask<IDownloadJob, AbsolutePath> downloadJob);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    IJobTask<IAddLocalFile, LocalFile.ReadOnly> AddLocalFile(AbsolutePath absolutePath);

    /// <summary>
    /// Installs a library item into a target loadout.
    /// </summary>
    /// <param name="libraryItem">The item to install.</param>
    /// <param name="targetLoadout">The target loadout.</param>
    /// <param name="parent">If specified the installed item will be placed in this group, otherwise it will default to the user's local collection</param>
    /// <param name="installer">The Library will use this installer to install the item</param>
    /// <param name="fallbackInstaller">Fallback installer instead of the default advanced installer</param>
    IJobTask<IInstallLoadoutItemJob, LoadoutItemGroup.ReadOnly> InstallItem(
        LibraryItem.ReadOnly libraryItem,
        LoadoutId targetLoadout,
        Optional<LoadoutItemGroupId> parent = default,
        ILibraryItemInstaller? installer = null,
        ILibraryItemInstaller? fallbackInstaller = null);

    /// <summary>
    /// Removes a number of items from the library.
    /// </summary>
    /// <param name="libraryItems">The items to remove from the library.</param>
    /// <param name="gcRunMode">Defines how the garbage collector should be run</param>
    Task RemoveItems(IEnumerable<LibraryItem.ReadOnly> libraryItems, GarbageCollectorRunMode gcRunMode = GarbageCollectorRunMode.RunAsyncInBackground);
}
