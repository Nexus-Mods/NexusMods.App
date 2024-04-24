using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public class FileOriginEntryViewModel : AViewModel<IFileOriginEntryViewModel>, IFileOriginEntryViewModel
{
    public required string Name { get; init; } 
    public string Version { get; init; } = "-";
    public required string Size { get; init; }
    public string Date { get; init; } = "-";
    public required ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
}
