using System.Reactive;
using System.Reactive.Disposables;
using Avalonia;
using NexusMods.UI.Sdk;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelResizerViewModel : AViewModel<IPanelResizerViewModel>, IPanelResizerViewModel
{
    [Reactive] public Point LogicalStartPoint { get; set; }
    [Reactive] public Point LogicalEndPoint { get; set; }
    [Reactive] public (Point ActualStartPoint, Point ActualEndPoint) ActualPoints { get; set; }

    public bool IsHorizontal { get; }

    public IReadOnlyList<PanelId> ConnectedPanels { get; }

    public ReactiveCommand<double, double> DragStartCommand { get; }

    public ReactiveCommand<Unit, Unit> DragEndCommand { get; }

    public PanelResizerViewModel(Point logicalStartPoint, Point logicalEndPoint, bool isHorizontal, IReadOnlyList<PanelId> connectedPanels)
    {
        LogicalStartPoint = logicalStartPoint;
        LogicalEndPoint = logicalEndPoint;
        IsHorizontal = isHorizontal;
        ConnectedPanels = connectedPanels;

        DragStartCommand = ReactiveCommand.Create<double, double>(input => input);
        DragEndCommand = ReactiveCommand.Create(() => { });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalStartPoint, vm => vm.LogicalEndPoint)
                .SubscribeWithErrorLogging(_ => UpdateActualPoints())
                .DisposeWith(disposables);
        });
    }

    private Size _workspaceSize = MathUtils.Zero;
    private void UpdateActualPoints()
    {
        var matrix = Matrix.CreateScale(_workspaceSize.Width, _workspaceSize.Height);

        ActualPoints = (LogicalStartPoint.Transform(matrix), LogicalEndPoint.Transform(matrix));
    }

    public void Arrange(Size workspaceSize)
    {
        _workspaceSize = workspaceSize;
        UpdateActualPoints();
    }
}
