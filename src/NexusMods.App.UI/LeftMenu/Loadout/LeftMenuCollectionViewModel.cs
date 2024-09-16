using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.LeftMenu.Items;

namespace NexusMods.App.UI.LeftMenu.Loadout;

public class LeftMenuCollectionViewModel : IconViewModel
{
    /// <summary>
    /// The group id of the collection.
    /// </summary>
    public required CollectionGroupId CollectionGroupId { get; init; }
}
