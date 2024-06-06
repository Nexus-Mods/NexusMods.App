using System.Reactive;
using NexusMods.Abstractions.FileStore.Downloads;
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
    DownloadAnalysis.Model FileOrigin { get; }
    ReactiveCommand<Unit, Unit> AddToLoadoutCommand { get; }
    ReactiveCommand<Unit, Unit> AddAdvancedToLoadoutCommand { get; }
    ReactiveCommand<NavigationInformation, Unit> ViewModCommand { get; }
}
