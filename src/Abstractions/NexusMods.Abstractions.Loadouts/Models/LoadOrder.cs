using JetBrains.Annotations;
using NexusMods.Abstractions.Loadouts.Attributes;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Represents a sorting of some sortable items in this Loadout.
/// A Game (and subsequently a Loadout) can have multiple LoadOrders of different types of sortable items.
/// <remarks>
/// This should 
///
/// </remarks>
/// </summary>
[PublicAPI]
public partial class LoadOrder : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.LoadOrder";
    
    /// <summary>
    /// The Loadout that this LoadOrder is associated with.
    /// </summary>
    public static readonly ReferenceAttribute<Loadout> Loadout = new(Namespace, nameof(Loadout))
    {
        IsIndexed = true,
    };
    
    /// <summary>
    /// Static Guid id of this load order type, to distinguish it from other load orders types used by the game.
    /// E.g. RedMod Load Order and .archive Load Order will have two different LoadOrderTypeIds. 
    /// </summary>
    public static readonly GuidAttribute LoadOrderTypeId = new(Namespace, nameof(LoadOrderTypeId));
    
}
