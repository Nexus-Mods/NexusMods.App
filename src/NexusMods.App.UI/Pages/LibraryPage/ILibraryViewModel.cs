using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LibraryItemModel>? Source { get; }

    bool IsEmpty { get; }

    R3.Subject<(LibraryItemModel, bool)> ActivationSubject { get; }

    R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }
}
