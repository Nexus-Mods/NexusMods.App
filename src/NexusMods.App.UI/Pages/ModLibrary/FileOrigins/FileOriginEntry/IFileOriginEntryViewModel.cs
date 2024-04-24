using System.Reactive;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public interface IFileOriginEntryViewModel : IViewModelInterface
{
    string Name { get; init;}
    string Version { get; init; }
    string Size { get; init; }    
    string Date { get; init; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; init; }
}
