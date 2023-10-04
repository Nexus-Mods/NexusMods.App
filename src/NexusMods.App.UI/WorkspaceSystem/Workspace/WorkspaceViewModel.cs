using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelViewModel> _panels = Initializers.ReadOnlyObservableCollection<IPanelViewModel>();
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    [Reactive]
    public ReactiveCommand<AddPanelInput, IPanelViewModel> AddPanelCommand { get; private set; } = Initializers.CreateReactiveCommand<AddPanelInput, IPanelViewModel>();

    [Reactive]
    public ReactiveCommand<RemovePanelInput, Unit> RemovePanelCommand { get; private set; } = Initializers.CreateReactiveCommand<RemovePanelInput>();

    public WorkspaceViewModel()
    {
        // TODO: setting?
        const int maxPanelCount = 4;

        this.WhenActivated(disposables =>
        {
            _panelSource
                .Connect()
                .Sort(PanelComparer.Instance)
                .Bind(out _panels)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            var canAddPanel = _panelSource.CountChanged.Select(count => count < maxPanelCount);
            AddPanelCommand = ReactiveCommand.Create<AddPanelInput, IPanelViewModel>(addPanelInput =>
            {
                var newPanelLogicalBounds = MathUtils.One;

                var (panelToSplit, splitVertically) = addPanelInput;
                if (panelToSplit is not null)
                {
                    var tuple = MathUtils.Split(panelToSplit.LogicalBounds, vertical: splitVertically);
                    panelToSplit.LogicalBounds = tuple.UpdatedLogicalBounds;
                    newPanelLogicalBounds = tuple.NewPanelLogicalBounds;
                }

                var panelViewModel = new PanelViewModel
                {
                    LogicalBounds = newPanelLogicalBounds
                };

                _panelSource.AddOrUpdate(panelViewModel);
                ArrangePanels(_lastWorkspaceControlSize);

                return panelViewModel;
            }, canAddPanel).DisposeWith(disposables);

            var canRemovePanel = _panelSource.CountChanged.Select(count => count > 1);
            RemovePanelCommand = ReactiveCommand.Create<RemovePanelInput, Unit>(removePanelInput =>
            {
                var (toConsume, toExpand) = removePanelInput;

                _panelSource.RemoveKey(toConsume.Id);
                toExpand.LogicalBounds = MathUtils.Join(toExpand.LogicalBounds, toConsume.LogicalBounds);

                return Unit.Default;
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
