using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Attributes;
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
    
    /// <summary>
    /// Reference to the Load Order id for this entry. We may expect the game addon to define
    ///  the identifier used by the game for the load order, but we still need to be able to
    ///  identify this sortable entry in the loadout.
    /// </summary>
    public static readonly GuidAttribute ItemId = new(Namespace, nameof(ItemId));
}
