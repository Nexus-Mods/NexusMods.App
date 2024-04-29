using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public class FileOriginEntryViewModel : AViewModel<IFileOriginEntryViewModel>, IFileOriginEntryViewModel
{
    public required string Name { get; init; } 
    public string Version { get; init; } = "-";
    public required Size Size { get; init; }
    public DateTime ArchiveDate { get; init; } = DateTime.UnixEpoch;
    
    public DateTime LastInstalledDate { get; init; } = DateTime.UnixEpoch;
    public required ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
}
