using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelButtonViewModel : AViewModel<IAddPanelButtonViewModel>, IAddPanelButtonViewModel
{
    public IReadOnlyDictionary<PanelId, Rect> NewLayoutState { get; }
    public IImage ButtonImage { get; }
    public ReactiveCommand<Unit, IReadOnlyDictionary<PanelId, Rect>> AddPanelCommand { get; }

    public AddPanelButtonViewModel(
        IReadOnlyDictionary<PanelId, Rect> newLayoutState,
        IImage buttonImage)
    {
        NewLayoutState = newLayoutState;
        ButtonImage = buttonImage;

        AddPanelCommand = ReactiveCommand.Create(() => NewLayoutState);
    }
}
