using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ILoadoutViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LoadoutItemModel>? Source { get; }
    bool IsEmpty { get; }

    R3.Subject<(LoadoutItemModel, bool)> ActivationSubject { get; }

    R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }

    R3.ReactiveCommand<R3.Unit> ViewFilesCommand { get; }

    R3.ReactiveCommand<R3.Unit> RemoveItemCommand { get; }
}
