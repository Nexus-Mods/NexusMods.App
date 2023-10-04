using Avalonia;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelViewModel : IViewModelInterface
{
    public PanelId Id { get; }

    public IViewModel? Content { get; set; }

    public Rect LogicalBounds { get; set; }

    public Rect ActualBounds { get; set; }

    public void Arrange(Size workspaceControlSize);
}
