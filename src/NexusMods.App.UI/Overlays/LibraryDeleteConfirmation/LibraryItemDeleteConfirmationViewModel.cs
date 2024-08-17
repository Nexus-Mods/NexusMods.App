namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public class LibraryItemDeleteConfirmationViewModel : AOverlayViewModel<ILibraryItemDeleteConfirmationViewModel, bool>, ILibraryItemDeleteConfirmationViewModel
{
    public required List<string> NonPermanentItems { get; init; }
    public required List<string> ManuallyAddedItems { get; init; }
    public required List<string> ItemsInLoadouts { get; init; }

    public LibraryItemDeleteConfirmationViewModel() { }

    public static LibraryItemDeleteConfirmationViewModel FromWarningDetector(LibraryItemDeleteWarningDetector detector)
    {
        var nonPermanentItems = new List<string>(detector.NonPermanentItems.Count);
        foreach (var item in detector.NonPermanentItems)
            nonPermanentItems.Add(item.Name);

        var manuallyAddedItems = new List<string>(detector.ManuallyAddedItems.Count);
        foreach (var item in detector.ManuallyAddedItems)
            manuallyAddedItems.Add(item.Name);

        var itemsInLoadouts = new List<string>(detector.ItemsInLoadouts.Count);
        foreach (var item in detector.ItemsInLoadouts)
            itemsInLoadouts.Add(item.Name);

        return new LibraryItemDeleteConfirmationViewModel()
        {
            ItemsInLoadouts = itemsInLoadouts,
            ManuallyAddedItems = manuallyAddedItems,
            NonPermanentItems = nonPermanentItems,
        };
    }
}
