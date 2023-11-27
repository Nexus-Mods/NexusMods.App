using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public class AddPanelButtonDesignViewModel : AddPanelButtonViewModel
{
    public AddPanelButtonDesignViewModel() : base(DummyState, IconUtils.StateToBitmap(DummyState)) { }

    private static readonly IReadOnlyDictionary<PanelId, Rect> DummyState = new Dictionary<PanelId, Rect>
    {
        { PanelId.NewId(), new Rect(0, 0, 0.5, 0.5) },
        { PanelId.DefaultValue, new Rect(0.5, 0, 0.5, 0.5) },
        { PanelId.NewId(), new Rect(0, 0.5, 0.5, 0.5) },
        { PanelId.NewId(), new Rect(0.5, 0.5, 0.5, 0.5) },
    };
}
