using System.Reactive.Joins;
using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Represents an item that is sorted in a Load Order
/// This should not be used directly, but rather be extended by the game-specific implementation.
/// 
/// Each implementation should provide the game specific identifier used to map entries of the Load Order to items of the loadout. 
/// E.g. the plugin name for Skyrim plugins, or the module uuid for BG3 pak files.
/// </summary>
[PublicAPI]
public partial class SortableEntry : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.SortableEntry";
    
    /// <summary>
    /// Reference to the Load Order that this item is part of.
    /// </summary>
    public static readonly ReferenceAttribute<SortOrder> ParentSortOrder = new(Namespace, nameof(ParentSortOrder))
    {
        IsIndexed = true,
    };
    
    /// <summary>
    /// The order in which this item should be loaded relative to other items in the Load Order.
    /// </summary>
    public static readonly Int32Attribute SortIndex = new(Namespace, nameof(SortIndex));

    static class Queries
    {
        /*
        /// <summary>
        /// Include all sort order entities that are associated with a loadout
        /// </summary>
        internal static readonly Flow<(EntityId Loadout, EntityId Entity)> LoadoutSortOrderSubFlow =
            Pattern.Create()
                .Db(out var sortOrder, SortOrder.LoadoutId, out var loadoutId)
                .Db(out var sortItem, ParentSortOrder, sortOrder)
                .Return(loadoutId, sortItem);
                */
    }

}
