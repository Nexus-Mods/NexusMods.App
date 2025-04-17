using DynamicData.Kernel;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Abstractions.Games.Interfaces;

/// <summary>
/// We will never know what TObject is, but we can make sure it has a couple of
///  constraints. 
/// </summary>
public interface ISortableDbEntryConstraints
{
    // This Id should match the Guid ID assigned to the sortable item
    //  we display to the user. (ISortableItem.ItemId)
    public Guid ItemId { get; }
    
    // db id. This can initially be null, but should be set once we start
    //  generating the SortableItems
    public Optional<EntityId> EntityId { get; }
    
    // The sort index for the TObject
    public int SortIndex { get; set; }
}
