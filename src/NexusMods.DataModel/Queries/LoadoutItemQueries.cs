using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Cascade;
using NexusMods.Cascade.Structures;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.ElementComparers;

namespace NexusMods.DataModel.Queries;

public static class LoadoutItemQueries
{
    /// <summary>
    /// A Key->Value relationship of each loadout item and each of its parent loadout items. The top value of everything
    /// is default (EntityId.MinValue).
    /// </summary>
    public static readonly Flow<KeyedValue<EntityId, EntityId>> LoadoutItemAncestors = LoadoutItem.Parent.Ancestors();

    /// <summary>
    /// If a loadout item has a disabled parent (or is itself disabled), it will have a keyed entry in this flow. The value
    /// is the ID of the disabled parent.
    /// </summary>
    public static readonly Flow<KeyedValue<EntityId, EntityId>> HasDisabledParent = LoadoutItemAncestors
         // swap key and value, so that we're parent->child
        .Select(row => new KeyedValue<EntityId, EntityId>(row.Value, row.Key))
        .LeftInnerJoin(LoadoutItem.Disabled)
        .Select(row => new KeyedValue<EntityId, EntityId>(row.Value.Item1, row.Key));

    /// <summary>
    /// A count of how many parents are disabled for each loadout item. This includes the item itself if it is disabled.
    /// </summary>
    public static readonly Flow<KeyedValue<EntityId, int>> DisabledParentCount = HasDisabledParent.Count();

    /// <summary>
    /// All the enabled loadout items.
    /// </summary>
    public static readonly Flow<EntityId> EnabledLoadoutItems =
        LoadoutItem.Loadout
            .LeftOuterJoin(DisabledParentCount)
            .Where(row => row.Value.Item2 == 0)
            .Select(row => row.Key);

    /// <summary>
    /// A count of all the enabled loadout items.
    /// </summary>
    public static readonly Flow<KeyedValue<int, int>> EnabledCount =
        EnabledLoadoutItems
            .Select(x => new KeyedValue<int, ulong>(0, x.Value))
            .Count();

    /// <summary>
    /// A pairing of loadout file paths and the winning loadout item for that path. 
    /// </summary>
    public static readonly Flow<KeyedValue<GamePath, EntityId>> WinningLoadoutFiles =
        EnabledLoadoutItems
            // Key each item
            .Rekey(id => id)
            // Join with a targeted path
            .LeftInnerJoin(LoadoutItemWithTargetPath.TargetPath)
            .Select(row => new KeyedValue<GamePath, EntityId>(new GamePath(row.Value.Item2.Item2, row.Value.Item2.Item3), row.Key))
            // For now we'll take the item with the highest ID
            .MaxBy(row => row);

}
