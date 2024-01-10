using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelResizerViewModel : IViewModelInterface
{
    public Point LogicalStartPoint { get; set; }
    public Point LogicalEndPoint { get; set; }

    public Point ActualStartPoint { get; set; }
    public Point ActualEndPoint { get; set; }

    /// <summary>
    /// Gets whether the resizer is horizontally aligned.
    /// </summary>
    public bool IsHorizontal { get; }

    /// <summary>
    /// Gets all connected panels that have to move with the resizer.
    /// </summary>
    public IReadOnlyList<PanelId> ConnectedPanels { get; }

    public void Arrange(Size workspaceSize);

    public ReactiveCommand<double, double> DragStartCommand { get; }

    /// <summary>
    /// Gets the command invoked by the resizer when the user stopped dragging the control.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DragEndCommand { get; }
}
