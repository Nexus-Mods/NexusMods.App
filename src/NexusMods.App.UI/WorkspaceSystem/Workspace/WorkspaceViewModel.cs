using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using ReactiveUI;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private const int Columns = 2;
    private const int Rows = 2;
    private const int MaxPanelCount = Columns * Rows;

    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IPanelViewModel> _panels;
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    private readonly SourceList<IAddPanelButtonViewModel> _addPanelButtonViewModelSource = new();
    private readonly ReadOnlyObservableCollection<IAddPanelButtonViewModel> _addPanelButtonViewModels;
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelButtonViewModels => _addPanelButtonViewModels;

    private readonly PageFactoryController _factoryController;
    public WorkspaceViewModel(PageFactoryController factoryController)
    {
        _factoryController = factoryController;

        var addPanelButtonViewModelsDisposable = _addPanelButtonViewModelSource
            .Connect()
            .DisposeMany()
            .Bind(out _addPanelButtonViewModels)
            .Subscribe();

        var panelDisposable = _panelSource
            .Connect()
            .DisposeMany()
            .Sort(PanelComparer.Instance)
            .Bind(out _panels)
            .Do(_ => UpdateStates())
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            addPanelButtonViewModelsDisposable.DisposeWith(disposables);
            panelDisposable.DisposeWith(disposables);

            _addPanelButtonViewModelSource
                .Connect()
                .MergeMany(item => item.AddPanelCommand)
                .SubscribeWithErrorLogging(nextState => AddPanel(nextState))
                .DisposeWith(disposables);

            _panelSource
                .Connect()
                .MergeMany(item => item.CloseCommand)
                .SubscribeWithErrorLogging(ClosePanel)
                .DisposeWith(disposables);
        });
    }

    private Size _lastWorkspaceSize;
    private bool IsHorizontal => _lastWorkspaceSize.Width > _lastWorkspaceSize.Height;

    /// <inheritdoc/>
    public void ArrangePanels(Size workspaceSize)
    {
        _lastWorkspaceSize = workspaceSize;
        foreach (var panelViewModel in Panels)
        {
            panelViewModel.Arrange(workspaceSize);
        }
    }

    /// <inheritdoc/>
    public void SwapPanels(IPanelViewModel first, IPanelViewModel second)
    {
        (second.LogicalBounds, first.LogicalBounds) = (first.LogicalBounds, second.LogicalBounds);
        ArrangePanels(_lastWorkspaceSize);
        UpdateStates();
    }

    private void UpdateStates()
    {
        _addPanelButtonViewModelSource.Edit(updater =>
        {
            updater.Clear();
            if (_panels.Count == MaxPanelCount) return;

            var states = GridUtils.GetPossibleStates(_panels, Columns, Rows);
            foreach (var state in states)
            {
                var image = IconUtils.StateToBitmap(state);
                updater.Add(new AddPanelButtonViewModel(state, image));
            }
        });
    }

    /// <inheritdoc/>
    public IPanelViewModel AddPanel(IReadOnlyDictionary<PanelId, Rect> state)
    {
        IPanelViewModel panelViewModel = null!;
        _panelSource.Edit(updater =>
        {
            foreach (var kv in state)
            {
                var (panelId, logicalBounds) = kv;
                if (panelId == PanelId.Empty)
                {
                    panelViewModel = new PanelViewModel(_factoryController)
                    {
                        LogicalBounds = logicalBounds,
                    };

                    panelViewModel.AddTab();
                    panelViewModel.Arrange(_lastWorkspaceSize);
                    updater.AddOrUpdate(panelViewModel);
                }
                else
                {
                    var existingPanel = updater.Lookup(panelId);
                    Debug.Assert(existingPanel.HasValue);

                    var value = existingPanel.Value;
                    value.LogicalBounds = logicalBounds;
                    updater.AddOrUpdate(value);
                }
            }
        });

        Debug.Assert(panelViewModel is not null);
        return panelViewModel;
    }

    /// <inheritdoc/>
    public void ClosePanel(PanelId panelToClose)
    {
        var currentState = _panels.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);
        var newState = GridUtils.GetStateWithoutPanel(currentState, panelToClose, isHorizontal: IsHorizontal);

        _panelSource.Edit(updater =>
        {
            updater.Remove(panelToClose);

            foreach (var kv in newState)
            {
                var (panelId, logicalBounds) = kv;
                {
                    var existingPanel = updater.Lookup(panelId);
                    Debug.Assert(existingPanel.HasValue);

                    var value = existingPanel.Value;
                    value.LogicalBounds = logicalBounds;
                    updater.AddOrUpdate(value);
                }
            }
        });
    }

    public WorkspaceData ToData()
    {
        var workspaceData = new WorkspaceData
        {
            Panels = _panels.Select(panel => panel.ToData()).ToArray()
        };

        return workspaceData;
    }

    public void FromData(WorkspaceData data)
    {
        _panelSource.Clear();

        _panelSource.Edit(updater =>
        {
            foreach (var panel in data.Panels)
            {
                var vm = new PanelViewModel(_factoryController);
                vm.FromData(panel);

                updater.AddOrUpdate(vm);
            }
        });
    }
}
