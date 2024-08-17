namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public class LibraryItemDeleteConfirmationViewModel : AOverlayViewModel<ILibraryItemDeleteConfirmationViewModel, bool>, ILibraryItemDeleteConfirmationViewModel
{
    public required List<string> NonPermanentItems { get; init; }
    public required List<string> ManuallyAddedItems { get; init; }
    public required List<string> ItemsInLoadouts { get; init; }
}
