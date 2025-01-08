using NexusMods.Abstractions.Loadouts;

namespace NexusMods.App.UI.LeftMenu.Items;

public class LeftMenuCollectionItemViewModel : IconViewModel
{
    /// <summary>
    /// The group id of the collection.
    /// </summary>
    public required CollectionGroupId CollectionGroupId { get; init; }
}
