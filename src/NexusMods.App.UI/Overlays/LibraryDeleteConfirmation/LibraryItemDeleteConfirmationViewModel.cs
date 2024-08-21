using NexusMods.App.UI.Pages.Library;
namespace NexusMods.App.UI.Overlays.LibraryDeleteConfirmation;

public class LibraryItemDeleteConfirmationViewModel : AOverlayViewModel<ILibraryItemDeleteConfirmationViewModel, bool>, ILibraryItemDeleteConfirmationViewModel
{
    public required List<string> AllItems { get; init; }
    public required List<LibraryItemUsedLoadoutInfo> LoadoutsUsed { get; init; }

    // ReSharper disable once EmptyConstructor
    public LibraryItemDeleteConfirmationViewModel() { }

    public static LibraryItemDeleteConfirmationViewModel FromWarningDetector(LibraryItemDeleteWarningDetector detector)
    {
        var allItems = new List<string>(detector.AllItems.Count);
        foreach (var item in detector.AllItems)
            allItems.Add(item.Name);
        
        return new LibraryItemDeleteConfirmationViewModel
        {
            AllItems = allItems,
            LoadoutsUsed = detector.LoadoutsUsed,
        };
    }
}
