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
public partial class SortableItemModel : IModelDefinition
{
    private const string Namespace = "NexusMods.Loadouts.SortableItemModel";
    
    /// <summary>
    /// Name of the item for display purposes.
    /// </summary>
    public static readonly StringAttribute Name = new(Namespace, nameof(Name));
    
    /// <summary>
    /// Reference to the Load Order that this item is part of.
    /// </summary>
    public static readonly ReferenceAttribute<LoadOrder> ParentLoadOrder = new(Namespace, nameof(ParentLoadOrder))
    {
        IsIndexed = true,
    };
    
    /// <summary>
    /// The order in which this item should be loaded relative to other items in the Load Order.
    /// </summary>
    public static readonly Int32Attribute SortIndex = new(Namespace, nameof(SortIndex));
    
}
