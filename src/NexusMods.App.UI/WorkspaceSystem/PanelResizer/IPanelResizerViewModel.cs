using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelResizerViewModel : IViewModelInterface
{
    public Point LogicalPosition { get; set; }

    public Point ActualPosition { get; set; }

    public bool IsHorizontal { get; }

    public PanelId[] ConnectedPanels { get; }

    public void Arrange(Size workspaceSize);

    public ReactiveCommand<Point, Point> DragCommand { get; }

    public ReactiveCommand<Unit, Unit> FinishDragCommand { get; }
}
