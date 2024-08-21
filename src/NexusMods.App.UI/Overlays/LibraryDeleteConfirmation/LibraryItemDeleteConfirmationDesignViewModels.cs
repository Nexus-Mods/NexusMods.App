using NexusMods.App.UI.Pages.Library;
namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public static class LibraryItemDeleteConfirmationDesignViewModels
{
    public static readonly LibraryItemDeleteConfirmationViewModel NotInAnyLoadout = new()
    {
        AllItems =
        [
            "Some Mod that's not in Any Loadout",
            "Some other Mod that's not in Any Loadout",
        ],
        LoadoutsUsed = [],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel InLoadoutsInSingleStardewValleyLoadout = new()
    {
        AllItems =
        [
            "Some Mod that's in a Loadout",
            "Another Mod that's in a Loadout",
        ],
        LoadoutsUsed =
        [
            new LibraryItemUsedLoadoutInfo
            {
                GameName = "Stardew Valley",
                LoadoutName = "Loadout A",
                IsOnlyLoadoutForGame = true,
            },
        ],
    };
    
    public static readonly LibraryItemDeleteConfirmationViewModel InLoadoutsInMultipleStardewValleyLoadout = new()
    {
        AllItems =
        [
            "Some Mod that's in a Loadout",
            "Another Mod that's in a Loadout",
        ],
        LoadoutsUsed =
        [
            new LibraryItemUsedLoadoutInfo
            {
                GameName = "Stardew Valley",
                LoadoutName = "Loadout A",
                IsOnlyLoadoutForGame = false,
            },

            new LibraryItemUsedLoadoutInfo
            {
                GameName = "Stardew Valley",
                LoadoutName = "Loadout B",
                IsOnlyLoadoutForGame = false,
            },
        ],
    };
}
