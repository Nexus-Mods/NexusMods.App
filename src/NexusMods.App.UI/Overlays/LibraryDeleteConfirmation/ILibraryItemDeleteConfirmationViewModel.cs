namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

/// <summary>
///     Interface for a view model for the confirmation dialog displayed
///     when a dangerous delete operation is about to be performed.
/// </summary>
public interface ILibraryItemDeleteConfirmationViewModel : IOverlayViewModel<bool>
{
    /// <summary>
    /// Gets the list of library items that are not guaranteed to be redownloadable.
    /// </summary>
    public List<string> NonPermanentItems { get; }

    /// <summary>
    /// Gets the list of library items that were manually added to the library.
    /// </summary>
    public List<string> ManuallyAddedItems { get; }

    /// <summary>
    /// Gets the list of library items that are currently part of one or more loadouts.
    /// </summary>
    public List<string> ItemsInLoadouts { get; }
}

