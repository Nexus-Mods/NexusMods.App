using NexusMods.MnemonicDB.Abstractions.IndexSegments;

namespace NexusMods.Abstractions.Loadouts.Extensions;

/// <summary>
/// Extensions for loadout items.
/// </summary>
public static class LoadoutItemExtensions
{
    /// <summary>
    /// Returns an IEnumerable of the LoadoutItem and all its parents, in a child-to-parent order.
    /// </summary>
    public static IEnumerable<LoadoutItem.ReadOnly> GetThisAndParents(this LoadoutItem.ReadOnly item)
    {
        while (true)
        {
            yield return item;

            // TODO: Fix this once we fix Attr.TryGet on value types
            if (item.Contains(LoadoutItem.Parent) && LoadoutItem.Parent.TryGet(item, out var parent))
            {
                item = LoadoutItem.Load(item.Db, parent);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Returns all the LoadoutFiles in the collection that are enabled.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public static IEnumerable<LoadoutFile.ReadOnly> GetEnabledLoadoutFiles(this Entities<LoadoutItem.ReadOnly> items)
    {
        return items.Where(i => i.IsEnabled())
            .OfTypeLoadoutItemWithTargetPath()
            .OfTypeLoadoutFile();
    }

    /// <summary>
    /// Returns true if the LoadoutItem is enabled and all its parents are enabled.
    /// </summary>
    public static bool IsEnabled(this LoadoutItem.ReadOnly item)
    {
        return !item.GetThisAndParents().Any(i => i.IsDisabled);
    }
}
