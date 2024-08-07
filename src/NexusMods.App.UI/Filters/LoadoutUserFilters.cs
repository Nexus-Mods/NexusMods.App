using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI;

/// <summary>
/// Filters for displaying the contents of a Loadout to the user.
/// </summary>
[PublicAPI]
public static class LoadoutUserFilters
{
    /// <summary>
    /// Returns whether the given loadout item should be shown to the user.
    /// </summary>
    public static bool ShouldShow(LoadoutItem.ReadOnly loadoutItem)
    {
        if (!loadoutItem.IsLoadoutItemGroup()) return false;

        if (!loadoutItem.TryGetAsLibraryLinkedLoadoutItem(out var linked)) return false;
        return LibraryUserFilters.ShouldShow(linked.LibraryItem);
    }
}
