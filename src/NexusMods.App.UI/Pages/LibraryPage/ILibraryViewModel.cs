using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LibraryItemModel>? Source { get; }

    bool IsEmpty { get; }

    Subject<(LibraryItemModel, bool)> ActivationSubject { get; }

    ReactiveCommand<Unit> SwitchViewCommand { get; }

    ReactiveCommand<Unit> InstallSelectedItemsCommand { get; }
    ReactiveCommand<Unit> InstallSelectedItemsWithAdvancedInstallerCommand { get; }

    ReactiveCommand<Unit> OpenFilePickerCommand { get; }
    ReactiveCommand<Unit> OpenNexusModsCommand { get; }
}
