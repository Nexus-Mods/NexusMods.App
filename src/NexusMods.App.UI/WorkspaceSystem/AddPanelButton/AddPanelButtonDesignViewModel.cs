using System.Reactive;
using Avalonia;
using Avalonia.Media;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelButtonDesignViewModel : AViewModel<IAddPanelButtonViewModel>, IAddPanelButtonViewModel
{
    public IReadOnlyDictionary<PanelId, Rect> NewLayoutState { get; }
    public IImage ButtonImage { get; }
    public ReactiveCommand<Unit, Unit> AddPanelCommand => Initializers.EnabledReactiveCommand;

    public AddPanelButtonDesignViewModel()
    {
        NewLayoutState = new Dictionary<PanelId, Rect>
        {
            { PanelId.New(), new Rect(0, 0, 0.5, 0.5) },
            { PanelId.Empty, new Rect(0.5, 0, 0.5, 0.5) },
            { PanelId.New(), new Rect(0, 0.5, 0.5, 0.5) },
            { PanelId.New(), new Rect(0.5, 0.5, 0.5, 0.5) },
        };

        ButtonImage = IconUtils.StateToBitmap(NewLayoutState);
    }
}
