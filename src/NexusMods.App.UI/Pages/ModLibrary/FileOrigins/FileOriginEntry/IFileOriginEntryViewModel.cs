using System.Reactive;
using NexusMods.Abstractions.FileStore.Downloads;
using NexusMods.Abstractions.Installers;
using NexusMods.App.UI.Controls.Navigation;
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
    bool IsModAddedToLoadout { get; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; }
    Task AddUsingInstallerToLoadout(IModInstaller? installer, CancellationToken token);
    ReactiveCommand<NavigationInformation, Unit> ViewModCommand { get; }
}
