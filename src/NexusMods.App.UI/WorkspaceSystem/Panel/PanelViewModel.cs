using System.Reactive.Disposables;
using Avalonia;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; } = PanelId.From(Guid.NewGuid());

    [Reactive]
    public IViewModel? Content { get; set; }

    [Reactive]
    public Rect LogicalBounds { get; set; }

    [Reactive]
    public Rect ActualBounds { get; set; }

    private Size _workspaceControlSize = new(0, 0);

    public PanelViewModel()
    {
        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);
        });
    }

    public void Arrange(Size workspaceControlSize)
    {
        Console.WriteLine(nameof(Arrange));
        _workspaceControlSize = workspaceControlSize;
        UpdateActualBounds();
    }

    private void UpdateActualBounds()
    {
        Console.WriteLine(nameof(UpdateActualBounds));
        ActualBounds = new Rect(
            x: LogicalBounds.X * _workspaceControlSize.Width,
            y: LogicalBounds.Y * _workspaceControlSize.Height,
            width: LogicalBounds.Width * _workspaceControlSize.Width,
            height: LogicalBounds.Height * _workspaceControlSize.Height
        );
    }
}
