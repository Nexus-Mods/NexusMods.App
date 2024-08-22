using NexusMods.App.UI.Pages.Library;
namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

/// <summary>
///     Interface for a view model for the confirmation dialog displayed
///     when a dangerous delete operation is about to be performed.
/// </summary>
public interface ILibraryItemDeleteConfirmationViewModel : IOverlayViewModel<bool>
{
    /// <summary>
    /// Names of all items that are about to be removed.
    /// </summary>
    public List<string> AllItems { get; }
    
    /// <summary>
    /// Listing of all loadouts that contain the mods listed in <see cref="AllItems"/>.
    /// </summary>
    public List<LibraryItemUsedLoadoutInfo> LoadoutsUsed { get; } 
}

