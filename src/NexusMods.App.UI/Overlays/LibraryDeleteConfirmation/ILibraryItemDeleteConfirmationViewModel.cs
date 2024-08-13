namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

/// <summary>
///     Interface for a view model for the confirmation dialog displayed
///     when a dangerous delete operation is about to be performed.
/// </summary>
public interface ILibraryItemDeleteConfirmationViewModel : IOverlayViewModel<bool>
{
    LibraryItemDeleteWarningDetector WarningDetector { get; set; }
}

