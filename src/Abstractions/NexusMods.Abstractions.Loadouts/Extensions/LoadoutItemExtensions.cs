using System.Reactive.Linq;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.IndexSegments;
using NexusMods.MnemonicDB.Abstractions.Query;

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

            if (LoadoutItem.Parent.TryGetValue(item, out var parent))
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
    /// Returns an IEnumerable of the LoadoutItem Id and all its parents Ids, in a child-to-parent order.
    /// </summary>
    public static IEnumerable<LoadoutItemId> GetThisAndParentIds(this LoadoutItem.ReadOnly model)
    {
        var db = model.Db;
        var itemId = model.LoadoutItemId;
        
        while (true)
        {
            yield return itemId;
            
            if (LoadoutItem.Parent.TryGetValue(model, out var parent))
            {
                itemId = parent;
                model = LoadoutItem.Load(db, itemId);
            }
            else
            {
                break;
            }
        }
    }

    /// <summary>
    /// Returns true if the LoadoutItem is a child of the LoadoutItemGroup.
    /// </summary>
    public static bool IsChildOf(this LoadoutItem.ReadOnly item, LoadoutItemGroupId groupId)
    {
        while (item.Contains(LoadoutItem.Parent))
        {
            var group = item.Parent;
            if (group.LoadoutItemGroupId == groupId)
                return true;
            item = group.AsLoadoutItem();
        }
        return false;
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
        var current = item;
    
        while (true)
        {
            if (current.IsDisabled)
                return false;
            
            if (!LoadoutItem.Parent.TryGetValue(current, out var parentId))
                break;
            
            current = LoadoutItem.Load(current.Db, parentId);
        }
    
        return true;
    }
    
    /// <summary>
    /// Returns an observable that emits true if the LoadoutItem and all its parents are enabled.
    /// </summary>
    public static IObservable<bool> IsEnabledObservable(this LoadoutItem.ReadOnly item, IConnection connection)
    {
        return item.GetThisAndParentIds()
            .Select(itemId => LoadoutItem.Observe(connection, itemId).Select(i => !i.IsDisabled))
            .CombineLatest(list => list.All(b => b));
    }
}
