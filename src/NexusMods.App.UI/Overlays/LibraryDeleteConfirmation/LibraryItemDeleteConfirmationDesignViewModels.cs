namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public static class LibraryItemDeleteConfirmationDesignViewModels
{
    public static readonly LibraryItemDeleteConfirmationViewModel NonPermanentItemsOnly = new()
    {
        NonPermanentItems = [
            "Experimental Prototype Mod II XRD V2 (1.0.0) 3 & Knuckles",
            "Some Test Mod you got from not Nexus",
        ],
        ManuallyAddedItems = [],
        ItemsInLoadouts = [],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel ManuallyAddedItemsOnly = new()
    {
        NonPermanentItems = [],
        ManuallyAddedItems = [
            "Some Cool Mod you added from Disk",
            "Another Cool Mod from Disk",
        ],
        ItemsInLoadouts = [],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel InLoadoutsOnly = new()
    {
        NonPermanentItems = [],
        ManuallyAddedItems = [],
        ItemsInLoadouts = [
            "Some Mod that's already Added to a Loadout",
            "Another Mod that's in a Loadout",
        ],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel AllItems = new()
    {
        NonPermanentItems = [
            "Experimental Prototype Mod II XRD V2 (1.0.0) 3 & Knuckles",
            "Some Test Mod you got from not Nexus",
        ],
        ManuallyAddedItems = [
            "Some Cool Mod you added from Disk",
            "Another Cool Mod from Disk",
        ],
        ItemsInLoadouts = [
            "Some Mod that's already Added to a Loadout",
            "Another Mod that's in a Loadout",
        ],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel NoItems = new()
    {
        NonPermanentItems = [],
        ManuallyAddedItems = [],
        ItemsInLoadouts = [],
    };
}
