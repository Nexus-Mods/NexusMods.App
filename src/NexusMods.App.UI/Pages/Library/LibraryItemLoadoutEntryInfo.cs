using NexusMods.App.UI.Resources;
namespace NexusMods.App.UI.Pages.Library;

public class LibraryItemUsedLoadoutInfo
{
    /// <summary>
    /// The name of the game that the loadout belongs to.
    /// </summary>
    public required string GameName { get; init; }
    
    /// <summary>
    /// The name of the loadout that contains the mods listed in the delete mod dialog.
    /// </summary>
    public required string LoadoutName { get; init; }
    
    /// <summary>
    /// Names of collections within this loadout contain the mods listed in the delete mod dialog.
    /// </summary>
    public List<string> CollectionNames { get; } = new() { Language.LoadoutLeftMenuViewModel_LoadoutGridEntry };
    
    /// <summary>
    /// This is true if the loadout is not the only loadout that contains the mods listed in the delete mod dialog.
    /// </summary>
    public required bool IsOnlyLoadoutForGame { get; init; }
    
    /// <summary>
    /// Returns a string like 'Used in Loadout: Stardew Valley - Loadout A'.
    /// </summary>
    public string UsedInLoadoutString => IsOnlyLoadoutForGame 
        ? string.Format(Language.LibraryItemDeleteConfirmation_UsedBy, GameName) 
        : string.Format(Language.LibraryItemDeleteConfirmation_UsedByWithLoadout, GameName, LoadoutName);
}
