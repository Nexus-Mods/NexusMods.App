using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using DynamicData.Aggregation;
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

    private readonly SourceList<IPanelResizerViewModel> _resizersSource = new();
    private readonly ReadOnlyObservableCollection<IPanelResizerViewModel> _resizers;
    public ReadOnlyObservableCollection<IPanelResizerViewModel> Resizers => _resizers;

    private readonly PageFactoryController _factoryController;
    public WorkspaceViewModel(PageFactoryController factoryController)
    {
        _factoryController = factoryController;

        _addPanelButtonViewModelSource
            .Connect()
            .Bind(out _addPanelButtonViewModels)
            .Subscribe();

        _resizersSource
            .Connect()
            .Bind(out _resizers)
            .Subscribe();

        _panelSource
            .Connect()
            .Sort(PanelComparer.Instance)
            .Bind(out _panels)
            .Do(_ => UpdateStates())
            .Do(_ => UpdateResizers())
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            // Adding a panel
            _addPanelButtonViewModelSource
                .Connect()
                .MergeMany(item => item.AddPanelCommand)
                .SubscribeWithErrorLogging(nextState => AddPanel(nextState))
                .DisposeWith(disposables);

            // Closing a panel
            _panelSource
                .Connect()
                .MergeMany(item => item.CloseCommand)
                .SubscribeWithErrorLogging(ClosePanel)
                .DisposeWith(disposables);

            // TODO: popout command

            // Disabling certain features when there is only one panel
            _panelSource
                .Connect()
                .Count()
                .Select(panelCount => panelCount > 1)
                .Do(hasMultiplePanels =>
                {
                    for (var i = 0; i < Panels.Count; i++)
                    {
                        Panels[i].IsNotAlone = hasMultiplePanels;
                    }
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // Finished Resizing
            _resizersSource
                .Connect()
                .MergeMany(item => item.DragEndCommand)
                .Do(_ => UpdateStates())
                .Subscribe()
                .DisposeWith(disposables);

            // Resizing
            _resizersSource
                .Connect()
                .MergeManyWithSource(item => item.DragStartCommand)
                .Where(tuple => tuple.Item2 != new Point(0, 0))
                .Do(tuple =>
                {
                    var (item, newActualPosition) = tuple;

                    const double minX = 0.3;
                    const double minY = 0.3;
                    const double maxX = 1 - minX;
                    const double maxY = 1 - minY;

                    var lastItemPosition = item.LogicalPosition;
                    var newLogicalPosition = new Point(
                        Math.Max(minX, Math.Min(maxX, newActualPosition.X / _lastWorkspaceSize.Width)),
                        Math.Max(minY, Math.Min(maxY, newActualPosition.Y / _lastWorkspaceSize.Height))
                    );

                    item.LogicalPosition = newLogicalPosition;

                    var isHorizontal = item.IsHorizontal;
                    var connectedPanelIds = item.ConnectedPanels;

                    var connectedPanels = connectedPanelIds
                        .Select(panelId => _panelSource.Lookup(panelId))
                        .Where(optional => optional.HasValue)
                        .Select(optional => optional.Value)
                        .Order(PanelComparer.Instance)
                        .ToArray();

                    // in case we skip an update, the tolerance for edge checking is higher than usual.
                    const double defaultTolerance = 0.05;

                    // move panels
                    foreach (var panel in connectedPanels)
                    {
                        var currentSize = panel.LogicalBounds;

                        Rect newPanelBounds;
                        if (isHorizontal)
                        {
                            // true if the resizer sits on the "top" edge of the panel
                            var isResizerYAligned = lastItemPosition.Y.IsCloseTo(currentSize.Y, tolerance: defaultTolerance);

                            // if the resizer sits on the "top" edge of the panel, we want to move the panel with the resizer
                            var newY = isResizerYAligned ? newLogicalPosition.Y : currentSize.Y;

                            var diff = isResizerYAligned
                                ? currentSize.Y - newLogicalPosition.Y
                                : newLogicalPosition.Y - currentSize.Bottom;

                            var newHeight = currentSize.Height + diff;

                            newPanelBounds = new Rect(
                                currentSize.X,
                                newY,
                                currentSize.Width,
                                newHeight
                            );
                        }
                        else
                        {
                            // true if the resizer sits on the "left" edge of the panel
                            var isResizerXAligned = lastItemPosition.X.IsCloseTo(currentSize.X, tolerance: defaultTolerance);

                            // if the resizer sits on the "left" edge of the panel, we want to move the panel with the resizer
                            var newX = isResizerXAligned ? newLogicalPosition.X : currentSize.X;

                            var diff = isResizerXAligned
                                ? currentSize.X - newLogicalPosition.X
                                : newLogicalPosition.X - currentSize.Right;

                            var newWidth = currentSize.Width + diff;

                            newPanelBounds = new Rect(
                                newX,
                                currentSize.Y,
                                newWidth,
                                currentSize.Height
                            );
                        }

                        panel.LogicalBounds = newPanelBounds;
                    }

                    // move other resizers
                    foreach (var resizer in Resizers)
                    {
                        if (ReferenceEquals(item, resizer)) continue;
                        if (resizer.IsHorizontal != isHorizontal) continue;
                        if (!item.ConnectedPanels.Intersect(resizer.ConnectedPanels).Any()) continue;

                        resizer.LogicalPosition = isHorizontal
                            ? resizer.LogicalPosition.WithY(newLogicalPosition.Y)
                            : resizer.LogicalPosition.WithX(newLogicalPosition.X);
                    }
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);
        });
    }

    // TODO: make this reactive
    private Size _lastWorkspaceSize;
    private bool IsHorizontal => _lastWorkspaceSize.Width > _lastWorkspaceSize.Height;

    /// <inheritdoc/>
    public void Arrange(Size workspaceSize)
    {
        _lastWorkspaceSize = workspaceSize;
        foreach (var panelViewModel in Panels)
        {
            panelViewModel.Arrange(workspaceSize);
        }

        foreach (var resizerViewModel in Resizers)
        {
            resizerViewModel.Arrange(workspaceSize);
        }
    }

    public void SwapPanels(IPanelViewModel first, IPanelViewModel second)
    {
        (second.LogicalBounds, first.LogicalBounds) = (first.LogicalBounds, second.LogicalBounds);
        Arrange(_lastWorkspaceSize);
        UpdateStates();
    }

    private void UpdateStates()
    {
        _addPanelButtonViewModelSource.Edit(updater =>
        {
            updater.Clear();
            if (_panels.Count == MaxPanelCount) return;

            var panels = _panels.ToImmutableDictionary(panel => panel.Id, panel => panel.LogicalBounds);
            var states = GridUtils.GetPossibleStates(panels, Columns, Rows);
            foreach (var state in states)
            {
                var image = IconUtils.StateToBitmap(state);
                updater.Add(new AddPanelButtonViewModel(state, image));
            }
        });
    }

    private void UpdateResizers()
    {
        _resizersSource.Edit(updater =>
        {
            updater.Clear();

            var currentState = _panels.ToImmutableDictionary(x => x.Id, x => x.LogicalBounds);
            var resizers = GridUtils.GetResizers(currentState);

            updater.AddRange(resizers.Select(info =>
            {
                var vm = new PanelResizerViewModel(info.LogicalPosition, info.IsHorizontal, info.ConnectedPanels);
                vm.Arrange(_lastWorkspaceSize);
                return vm;
            }));
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
                if (panelId == PanelId.DefaultValue)
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
                }
            }
        });

        Debug.Assert(panelViewModel is not null);
        return panelViewModel;
    }

    public void ClosePanel(PanelId panelToClose)
    {
        var currentState = WorkspaceGridState.From(_panels, IsHorizontal);
        var newState = GridUtils.GetStateWithoutPanel(currentState, panelToClose);

        _panelSource.Edit(updater =>
        {
            updater.Remove(panelToClose);

            foreach (var panelState in newState)
            {
                var (panelId, logicalBounds) = panelState;
                {
                    var existingPanel = updater.Lookup(panelId);
                    Debug.Assert(existingPanel.HasValue);

                    var value = existingPanel.Value;
                    value.LogicalBounds = logicalBounds;
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
