using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.NexusModsLibrary;
using NexusMods.Sdk.Library;

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
/// <param name="Loadouts">The loadouts that this library item is used within.</param>
public record struct LibraryItemRemovalInfo(bool IsNexus, bool IsNonPermanent, bool IsManuallyAdded, Loadout.ReadOnly[] Loadouts)
{
    public static LibraryItemRemovalInfo Determine(LibraryItem.ReadOnly toRemove, Loadout.ReadOnly[] loadouts)
    {
        var info = new LibraryItemRemovalInfo();

        // Check if it's a file which was downloaded.
        if (toRemove.TryGetAsNexusModsLibraryItem(out _))
        {
            info.IsNexus = true;
            info.IsNonPermanent = !info.IsNexus;
        }
        else if (toRemove.TryGetAsLocalFile(out _))
        {
            info.IsManuallyAdded = true;
        }
        
        // Check if it's added to any loadout
        info.Loadouts = loadouts.Where(loadout => loadout.GetLoadoutItemsByLibraryItem(toRemove).Any()).ToArray();
        return info;
    }
}
