using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelButtonDesignViewModel : AddPanelButtonViewModel
{
    public AddPanelButtonDesignViewModel() : base(DummyState, IconUtils.StateToBitmap(DummyState)) { }

    private static readonly WorkspaceGridState DummyState = WorkspaceGridState.From(
        isHorizontal: true,
        new PanelGridState(PanelId.NewId(), new Rect(0, 0, 0.5, 0.5)),
        new PanelGridState(PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5)),
        new PanelGridState(PanelId.NewId(), new Rect(0.5, 0, 0.5, 0.5)),
        new PanelGridState(PanelId.DefaultValue, new Rect(0.5, 0.5, 0.5, 0.5))
    );
}
