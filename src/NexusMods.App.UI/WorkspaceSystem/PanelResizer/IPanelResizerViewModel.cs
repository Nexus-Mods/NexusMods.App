using System.Reactive;
using Avalonia;
using NexusMods.UI.Sdk;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public interface IPanelResizerViewModel : IViewModelInterface
{
    /// <summary>
    /// Gets or sets the logical start position of the resizer in the workspace.
    /// </summary>
    /// <remarks>
    /// Logical positions range from 0.0 to 1.0. A logical position of
    /// X=0.5 and Y=0.5 will be in the center of the workspace.
    /// </remarks>
    public Point LogicalStartPoint { get; set; }

    /// <summary>
    /// Gets or sets the logical end position of the resizer in the workspace.
    /// </summary>
    /// <remarks>
    /// Logical positions range from 0.0 to 1.0. A logical position of
    /// X=0.5 and Y=0.5 will be in the center of the workspace.
    /// </remarks>
    public Point LogicalEndPoint { get; set; }
    
    /// <summary>
    /// The actual start and end points of the resizer in the workspace.
    /// </summary>
    public (Point ActualStartPoint, Point ActualEndPoint) ActualPoints { get; set; }

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
