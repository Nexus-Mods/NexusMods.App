using System.Reactive.Disposables;
using Avalonia;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelResizerViewModel : AViewModel<IPanelResizerViewModel>, IPanelResizerViewModel
{
    [Reactive] public Point LogicalPosition { get; set; }
    [Reactive] public Point ActualPosition { get; set; }

    public bool IsHorizontal { get; }
    public PanelId[] ConnectedPanels { get; }

    public PanelResizerViewModel(Point logicalPosition, bool isHorizontal, PanelId[] connectedPanels)
    {
        LogicalPosition = logicalPosition;
        IsHorizontal = isHorizontal;
        ConnectedPanels = connectedPanels;

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalPosition)
                .SubscribeWithErrorLogging(_ => UpdateActualPosition())
                .DisposeWith(disposables);
        });
    }

    private Size _workspaceSize = MathUtils.Zero;
    private void UpdateActualPosition()
    {
        var x = LogicalPosition.X * _workspaceSize.Width;
        var y = LogicalPosition.Y * _workspaceSize.Height;
        ActualPosition = new Point(x, y);
    }

    public void Arrange(Size workspaceSize)
    {
        _workspaceSize = workspaceSize;
        UpdateActualPosition();
    }
}
