using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.MnemonicDB.Abstractions;
namespace NexusMods.App.UI.Pages.Library;

/// <summary>
///     Represents the properties needed for deletion of a library item.
/// </summary>
/// <param name="IsNexus">Whether the library item has been sourced from Nexus Mods (it is redownloadable).</param>
/// <param name="IsNonPermanent">
///     Whether the library item is a download that is not guaranteed to be redownloadable.
///     (As of time of writing it means 'not from Nexus Mods')
/// </param>
/// <param name="IsManuallyAdded">Whether this library item was manually added from FileSystem to library.</param>
/// <param name="IsAddedToAnyLoadout">True if this item has been added to any loadout.</param>
public record struct LibraryItemRemovalInfo(bool IsNexus, bool IsNonPermanent, bool IsManuallyAdded, bool IsAddedToAnyLoadout)
{
    public static LibraryItemRemovalInfo Determine(IConnection connection, LibraryItem.ReadOnly libraryItem, Loadout.ReadOnly[] loadouts)
    {
        var info = new LibraryItemRemovalInfo();

        // Check if it's a file which was downloaded.
        var isDownloadedFile = libraryItem.TryGetAsDownloadedFile(out var downloadedFile);;
        switch (isDownloadedFile)
        {
            case true:
                info.IsNexus = downloadedFile.IsNexusModsLibraryFile();
                info.IsNonPermanent = !info.IsNexus;
                break;
            // Check if it's a LocalFile (manually added)
            case false when libraryItem.TryGetAsLocalFile(out _):
                info.IsManuallyAdded = true;
                break;
        }

        // Check if it's added to any loadout
        info.IsAddedToAnyLoadout = loadouts.Any(loadout => loadout.GetLoadoutItemsByLibraryItem(libraryItem).Any());
        return info;
    }
}
