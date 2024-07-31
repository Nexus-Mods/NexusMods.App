using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Paths;
using LibraryItem = NexusMods.Abstractions.Library.Models.LibraryItem;

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
    /// <param name="installer">If set, must be set to a ILibraryItemInstaller (untyped here as to not require a library reference). The Library will use this installer to install the item</param>
    IJob InstallItem(LibraryItem.ReadOnly libraryItem, Loadout.ReadOnly targetLoadout, object? installer = null);
}
