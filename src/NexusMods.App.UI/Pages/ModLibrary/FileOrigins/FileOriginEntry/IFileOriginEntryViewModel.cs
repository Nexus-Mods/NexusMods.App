using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public interface IFileOriginEntryViewModel : IViewModelInterface
{
    string Name { get; init;}
    string Version { get; init; }
    Size Size { get; init; }    
    DateTime ArchiveDate { get; init; }
    string DisplayArchiveDate { get; }
    
    DateTime LastInstalledDate { get; set; }
    
    string DisplayLastInstalledDate { get; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
}
