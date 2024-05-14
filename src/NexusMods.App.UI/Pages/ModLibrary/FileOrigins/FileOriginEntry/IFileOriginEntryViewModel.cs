using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public interface IFileOriginEntryViewModel : IViewModelInterface
{
    string Name { get;}
    string Version { get; }
    Size Size { get; }    
    
    public DateTime LastInstalledDate { get; set; }
    public DateTime ArchiveDate { get; set; }
    string DisplayArchiveDate { get; }
    string DisplayLastInstalledDate { get; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; }
}
