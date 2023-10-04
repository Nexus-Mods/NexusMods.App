using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelViewModel> _panels = Initializers.ReadOnlyObservableCollection<IPanelViewModel>();
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    public ReactiveCommand<Unit, Unit> AddPanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;
    public ReactiveCommand<Unit, Unit> RemovePanelCommand { get; private set; } = Initializers.DisabledReactiveCommand;

    public WorkspaceViewModel()
    {
        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            var canAddPanel = _panelSource.CountChanged.Select(count => count < 4);
            AddPanelCommand = ReactiveCommand.Create(() =>
            {
                var newPanelLogicalBounds = new Rect(0.0, 0.0, 1.0, 1.0);

                var lastPanel = Panels.LastOrDefault();
                if (lastPanel is not null)
                {
                    var lastLogicalBounds = lastPanel.LogicalBounds;
                    var newWidth = lastLogicalBounds.Width / 2;

                    newPanelLogicalBounds = lastLogicalBounds.WithWidth(newWidth).WithX(lastLogicalBounds.Left + newWidth);
                    lastPanel.LogicalBounds = lastLogicalBounds.WithWidth(newWidth);
                }

                Console.WriteLine(newPanelLogicalBounds.ToString());
                _panelSource.AddOrUpdate(new PanelViewModel
                {
                    LogicalBounds = newPanelLogicalBounds
                });

                ArrangePanels(_lastWorkspaceControlSize);
            }, canAddPanel).DisposeWith(disposables);

            var canRemovePanel = _panelSource.CountChanged.Select(count => count > 0);
            RemovePanelCommand = ReactiveCommand.Create(() =>
            {
                _panelSource.RemoveKey(_panelSource.Keys.Last());
            }, canRemovePanel);
        });
    }

    private Size _lastWorkspaceControlSize;
    public void ArrangePanels(Size workspaceControlSize)
    {
        _lastWorkspaceControlSize = workspaceControlSize;
        foreach (var panelViewModel in _panelSource.Items)
        {
            panelViewModel.Arrange(workspaceControlSize);
        }
    }
}
