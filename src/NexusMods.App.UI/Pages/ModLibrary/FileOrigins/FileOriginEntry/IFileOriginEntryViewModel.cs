using System.Reactive;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.Paths;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.ModLibrary.FileOriginEntry;

public interface IFileOriginEntryViewModel : IViewModelInterface
{
    string Name { get;}
    string Version { get; }
    Size Size { get; }    
    string DisplayArchiveDate { get; }
    string DisplayLastInstalledDate { get; }
    ReactiveCommand<IModInstaller?, Unit> AddToLoadoutCommand { get; }
    bool IsModAddedToLoadout { get; }
}
