using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryViewModel : IPageViewModelInterface
{
    LibraryTreeDataGridAdapter Adapter { get; }
    ReadOnlyObservableCollection<ICollectionCardViewModel> Collections { get; }

    string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit> UpdateAllCommand { get; }
    public ReactiveCommand<Unit> RefreshUpdatesCommand { get; }
    ReactiveCommand<Unit> SwitchViewCommand { get; }

    ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }
    ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }
    ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }

    ReactiveCommand<Unit> OpenFilePickerCommand { get; }
    ReactiveCommand<Unit> OpenNexusModsCommand { get; }

    IStorageProvider? StorageProvider { get; set; }
}
