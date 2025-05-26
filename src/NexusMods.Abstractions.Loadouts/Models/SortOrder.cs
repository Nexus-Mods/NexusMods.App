using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a sorting of some sortable items in this Loadout.
/// A Game (and subsequently a Loadout) can have multiple SortOrders of different types of sortable items.
/// </summary>
[PublicAPI]
public partial class SortOrder : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.SortOrder";
    
    /// <summary>
    /// The Loadout that this SortOrder is associated with.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout))
    {
        IsIndexed = true,
    };
    
    /// <summary>
    /// The parent Loadout or Collection entity that this SortOrder belongs to.
    /// </summary>
    public static readonly LoadoutOrCollectionAttribute ParentEntity = new(Namespace, nameof(ParentEntity))
    {
        IsIndexed = true,
    };
    
    /// <summary>
    /// Static Guid id of this sort order type, to distinguish it from other sort orders types used by the game.
    /// E.g. RedMod Load Order and .archive load order will have two different SortOrderTypeIds. 
    /// </summary>
    public static readonly GuidAttribute SortOrderTypeId = new(Namespace, nameof(SortOrderTypeId));
    
}
