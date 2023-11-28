using System.Reactive;
using Avalonia;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelResizerViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the logical position of the resizer in the workspace.
    /// </summary>
    /// <remarks>
    /// Logical positions range from 0.0 to 1.0. A logical position of
    /// X=0.5 and Y=0.5 will be in the center of the workspace.
    /// </remarks>
    public Point LogicalPosition { get; set; }

    /// <summary>
    /// Gets or sets the actual position of the resizer in the workspace.
    /// </summary>
    /// <remarks>
    /// This gets calculated from the <see cref="LogicalPosition"/> using
    /// <see cref="Arrange"/>.
    /// </remarks>
    public Point ActualPosition { get; set; }

    /// <summary>
    /// Gets whether the resizer is horizontally aligned.
    /// </summary>
    public bool IsHorizontal { get; }

    /// <summary>
    /// Gets all connected panels that have to move with the resizer.
    /// </summary>
    public PanelId[] ConnectedPanels { get; }

    /// <summary>
    /// Arranges the resizer with the size of the workspace by
    /// updating the <see cref="ActualPosition"/>.
    /// </summary>
    public void Arrange(Size workspaceSize);

    /// <summary>
    /// Gets the command invoked by the resizer when the user drags the control around.
    /// </summary>
    /// <remarks>
    /// This will return the current position of the control.
    /// </remarks>
    public ReactiveCommand<Point, Point> DragStartCommand { get; }

    /// <summary>
    /// Gets the command invoked by the resizer when the user stopped dragging the control.
    /// </summary>
    public ReactiveCommand<Unit, Unit> DragEndCommand { get; }
}
