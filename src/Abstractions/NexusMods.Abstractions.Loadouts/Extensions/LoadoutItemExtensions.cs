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
            
            if (LoadoutItem.Parent.TryGet(item, out var parent))
            {
                item = LoadoutItem.Load(item.Db, parent);
            }
            else
            {
                break;
            }
        }
    }
}
