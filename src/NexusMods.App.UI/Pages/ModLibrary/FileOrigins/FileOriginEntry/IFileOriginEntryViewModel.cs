using System.Reactive;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public interface IFileOriginEntryViewModel : IViewModelInterface
{
    string Name { get; init;}
    string Version { get; init; }
    Size Size { get; init; }    
    string DisplayArchiveDate { get; }
    string DisplayLastInstalledDate { get; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
}
