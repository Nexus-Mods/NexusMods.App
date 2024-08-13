namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public class LibraryItemDeleteConfirmationViewModel : AOverlayViewModel<ILibraryItemDeleteConfirmationViewModel, bool>, ILibraryItemDeleteConfirmationViewModel
{
    public required LibraryItemDeleteWarningDetector WarningDetector { get; set; } = default;
}
