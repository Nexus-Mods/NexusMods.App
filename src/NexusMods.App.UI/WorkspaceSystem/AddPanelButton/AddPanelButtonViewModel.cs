using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelButtonViewModel : AViewModel<IAddPanelButtonViewModel>, IAddPanelButtonViewModel
{
    public IReadOnlyDictionary<PanelId, Rect> NewLayoutState { get; }
    public IImage ButtonImage { get; }
    public ReactiveCommand<Unit, Unit> AddPanelCommand { get; }

    public AddPanelButtonViewModel(
        IWorkspaceViewModel workspaceViewModel,
        IReadOnlyDictionary<PanelId, Rect> newLayoutState,
        IImage buttonImage)
    {
        NewLayoutState = newLayoutState;
        ButtonImage = buttonImage;

        AddPanelCommand = ReactiveCommand.Create(() =>
        {
            workspaceViewModel.AddPanel(NewLayoutState);
        });
    }
}
