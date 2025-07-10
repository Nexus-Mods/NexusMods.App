using System.Collections.ObjectModel;
using Avalonia.Platform.Storage;
using NexusMods.Abstractions.Loadouts;
using NexusMods.App.UI.Pages.LibraryPage.Collections;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public record InstallationTarget(CollectionGroupId Id, string Name);

public interface ILibraryViewModel : IPageViewModelInterface
{
    LibraryTreeDataGridAdapter Adapter { get; }
    ReadOnlyObservableCollection<ICollectionCardViewModel> Collections { get; }

    ReadOnlyObservableCollection<InstallationTarget> InstallationTargets { get; }
    InstallationTarget? SelectedInstallationTarget { get; set; }

    string EmptyLibrarySubtitleText { get; }

    public ReactiveCommand<Unit> UpdateAllCommand { get; }
    public ReactiveCommand<Unit> RefreshUpdatesCommand { get; }

    ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }
    ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }
    ReactiveCommand<Unit> UpdateSelectedItemsCommand { get; }
    ReactiveCommand<Unit> UpdateAndKeepOldSelectedItemsCommand { get; }
    ReactiveCommand<Unit> RemoveSelectedItemsCommand { get; }
    ReactiveCommand<Unit> DeselectItemsCommand { get; }

    public int SelectionCount { get; } 
    public int UpdatableSelectionCount { get; }
    public bool HasAnyUpdatesAvailable { get; }
    public bool IsUpdatingAll { get; }
    ReactiveCommand<Unit> OpenFilePickerCommand { get; }
    ReactiveCommand<Unit> OpenNexusModsCommand { get; }
    ReactiveCommand<Unit> OpenNexusModsCollectionsCommand { get; }
    
    IStorageProvider? StorageProvider { get; set; }
}
