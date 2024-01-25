using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using DynamicData.Aggregation;
using NexusMods.App.UI.Extensions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private const int MaxColumns = 2;
    private const int MaxRows = 2;
    private const int MaxPanelCount = MaxColumns * MaxRows;

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
            // Workspace resizing
            this.WhenAnyValue(vm => vm.IsHorizontal)
                .Distinct()
                .Do(_ => UpdateStates())
                .Do(_ => UpdateResizers())
                .Subscribe();

            // Adding a panel
            _addPanelButtonViewModelSource
                .Connect()
                .MergeMany(item => item.AddPanelCommand)
                .SubscribeWithErrorLogging(AddPanelWithDefaultTab)
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
                .Do(_ => UpdateResizers())
                .Subscribe()
                .DisposeWith(disposables);

            // Resizing
            _resizersSource
                .Connect()
                .MergeManyWithSource(item => item.DragStartCommand)
                .Where(tuple => tuple.Item2 != 0.0)
                .Do(tuple =>
                {
                    var (item, newValue) = tuple;

                    const double minX = 0.3;
                    const double minY = 0.3;
                    const double maxX = 1 - minX;
                    const double maxY = 1 - minY;

                    var isHorizontal = item.IsHorizontal;
                    var connectedPanelIds = item.ConnectedPanels;

                    var lastLogicalValue = GetValue(item.LogicalStartPoint, isHorizontal);
                    var newLogicalValue = isHorizontal
                        ? Math.Max(minY, Math.Min(maxY, newValue / _lastWorkspaceSize.Height))
                        : Math.Max(minX, Math.Min(maxX, newValue / _lastWorkspaceSize.Width));

                    item.LogicalStartPoint = WithValue(item.LogicalStartPoint, newLogicalValue, isHorizontal);
                    item.LogicalEndPoint = WithValue(item.LogicalEndPoint, newLogicalValue, isHorizontal);

                    var connectedPanels = connectedPanelIds
                        .Select(panelId => _panelSource.Lookup(panelId))
                        .Where(optional => optional.HasValue)
                        .Select(optional => optional.Value)
                        .Order(PanelComparer.Instance)
                        .ToArray();

                    // In case we skip an update, the tolerance for edge checking is higher than usual.
                    const double defaultTolerance = 0.05;

                    // move panels
                    foreach (var panel in connectedPanels)
                    {
                        var currentSize = panel.LogicalBounds;

                        Rect newPanelBounds;
                        if (isHorizontal)
                        {
                            // true if the resizer sits on the "top" edge of the panel
                            var isResizerYAligned = lastLogicalValue.IsCloseTo(currentSize.Y, tolerance: defaultTolerance);

                            // if the resizer sits on the "top" edge of the panel, we want to move the panel with the resizer
                            var newY = isResizerYAligned ? newLogicalValue : currentSize.Y;

                            var diff = isResizerYAligned
                                ? currentSize.Y - newLogicalValue
                                : newLogicalValue - currentSize.Bottom;

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
                            var isResizerXAligned = lastLogicalValue.IsCloseTo(currentSize.X, tolerance: defaultTolerance);

                            // if the resizer sits on the "left" edge of the panel, we want to move the panel with the resizer
                            var newX = isResizerXAligned ? newLogicalValue : currentSize.X;

                            var diff = isResizerXAligned
                                ? currentSize.X - newLogicalValue
                                : newLogicalValue - currentSize.Right;

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
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);
        });
    }

    private static double GetValue(Point point, bool isHorizontal) => isHorizontal ? point.Y : point.X;

    private static Point WithValue(Point point, double value, bool isHorizontal)
    {
        return isHorizontal ? point.WithY(value) : point.WithX(value);
    }

    private Size _lastWorkspaceSize;

    [Reactive] public bool IsHorizontal { get; private set; }

    /// <inheritdoc/>
    public void Arrange(Size workspaceSize)
    {
        _lastWorkspaceSize = workspaceSize;
        IsHorizontal = _lastWorkspaceSize.Width > _lastWorkspaceSize.Height;

        foreach (var panelViewModel in Panels)
        {
            panelViewModel.Arrange(workspaceSize);
        }

        foreach (var resizerViewModel in Resizers)
        {
            resizerViewModel.Arrange(workspaceSize);
        }
    }

    public void SwapPanels(PanelId firstPanelId, PanelId secondPanelId)
    {
        if (!_panelSource.Lookup(firstPanelId).TryGet(out var firstPanel)) return;
        if (!_panelSource.Lookup(secondPanelId).TryGet(out var secondPanel)) return;

        (secondPanel.LogicalBounds, firstPanel.LogicalBounds) = (firstPanel.LogicalBounds, secondPanel.LogicalBounds);
        Arrange(_lastWorkspaceSize);
        UpdateStates();
        UpdateResizers();
    }

    private void UpdateStates()
    {
        _addPanelButtonViewModelSource.Edit(updater =>
        {
            updater.Clear();
            if (_panels.Count == MaxPanelCount) return;

            var currentState = WorkspaceGridState.From(_panels, IsHorizontal);
            var newStates = GridUtils.GetPossibleStates(currentState, MaxColumns, MaxRows);

            foreach (var state in newStates)
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

            var currentState =WorkspaceGridState.From(_panels, IsHorizontal);
            var resizers = GridUtils.GetResizers(currentState);

            updater.AddRange(resizers.Select(info =>
            {
                var vm = new PanelResizerViewModel(info.Start, info.End, info.IsHorizontal, info.ConnectedPanels);
                vm.Arrange(_lastWorkspaceSize);
                return vm;
            }));
        });
    }

    public void AddPanelWithDefaultTab(WorkspaceGridState newWorkspaceState)
    {
        var allDetails = _factoryController.GetAllDetails().ToArray();
        var pageData = new PageData
        {
            FactoryId = NewTabPageFactory.StaticId,
            Context = new NewTabPageContext
            {
                DiscoveryDetails = allDetails
            }
        };

        AddPanelWithCustomTab(newWorkspaceState, pageData);
    }

    public void AddPanelWithCustomTab(WorkspaceGridState newWorkspaceState, PageData pageData)
    {
        _panelSource.Edit(updater =>
        {
            foreach (var panel in newWorkspaceState)
            {
                var (panelId, logicalBounds) = panel;
                if (panelId == PanelId.DefaultValue)
                {
                    var panelViewModel = new PanelViewModel(_factoryController)
                    {
                        LogicalBounds = logicalBounds,
                    };

                    panelViewModel.AddCustomTab(pageData);
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
