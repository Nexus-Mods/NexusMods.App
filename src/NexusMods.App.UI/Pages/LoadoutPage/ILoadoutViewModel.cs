using Avalonia.Controls;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public interface ILoadoutViewModel : IPageViewModelInterface
{
    ITreeDataGridSource<LoadoutItemModel>? Source { get; }

    R3.ReactiveCommand<R3.Unit> SwitchViewCommand { get; }
}
