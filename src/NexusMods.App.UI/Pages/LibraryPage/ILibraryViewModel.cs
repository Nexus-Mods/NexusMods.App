using System.Reactive;
using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LibraryItemModel>? Source { get; }

    R3.Subject<(LibraryItemModel, bool)> ActivationSubject { get; }

    ReactiveCommand<Unit, Unit> SwitchViewCommand { get; }
}
