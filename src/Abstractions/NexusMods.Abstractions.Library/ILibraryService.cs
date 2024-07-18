using JetBrains.Annotations;
using NexusMods.Abstractions.Downloads;
using NexusMods.Abstractions.Jobs;
using NexusMods.Abstractions.LibraryModels;
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
    IJob AddDownload(IDownloadJob downloadJob);

    /// <summary>
    /// Adds a local file to the library.
    /// </summary>
    IJob AddLocalFile(AbsolutePath absolutePath);

    /// <summary>
    /// Installs a library item into a target loadout.
    /// </summary>
    /// <param name="libraryItem">The item to install.</param>
    /// <param name="targetLoadout">The target loadout.</param>
    IJob InstallItem(LibraryItem.ReadOnly libraryItem, Loadout.ReadOnly targetLoadout);
}
