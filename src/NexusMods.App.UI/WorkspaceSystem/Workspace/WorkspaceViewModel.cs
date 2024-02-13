using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class WorkspaceViewModel : AViewModel<IWorkspaceViewModel>, IWorkspaceViewModel
{
    private const int MaxColumns = 2;
    private const int MaxRows = 2;
    private const int MaxPanelCount = MaxColumns * MaxRows;

    /// <inheritdoc/>
    public WorkspaceId Id { get; } = WorkspaceId.NewId();

    /// <inheritdoc/>
    public WindowId WindowId => _workspaceController.WindowId;

    /// <inheritdoc/>
    public IWorkspaceContext Context { get; set; } = EmptyContext.Instance;

    [Reactive] public bool IsActive { get; set; }

    private readonly SourceCache<IPanelViewModel, PanelId> _panelSource = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IPanelViewModel> _panels;
    public ReadOnlyObservableCollection<IPanelViewModel> Panels => _panels;

    private readonly SourceList<IAddPanelButtonViewModel> _addPanelButtonViewModelSource = new();
    private readonly ReadOnlyObservableCollection<IAddPanelButtonViewModel> _addPanelButtonViewModels;
    public ReadOnlyObservableCollection<IAddPanelButtonViewModel> AddPanelButtonViewModels => _addPanelButtonViewModels;

    private readonly SourceList<IPanelResizerViewModel> _resizersSource = new();
    private readonly ReadOnlyObservableCollection<IPanelResizerViewModel> _resizers;
    public ReadOnlyObservableCollection<IPanelResizerViewModel> Resizers => _resizers;

    private readonly IWorkspaceController _workspaceController;
    private readonly PageFactoryController _factoryController;

    public WorkspaceViewModel(
        IWorkspaceController workspaceController,
        PageFactoryController factoryController,
        Action<WorkspaceViewModel> unregisterFunc)
    {
        _workspaceController = workspaceController;
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
                            var isResizerYAligned =
                                lastLogicalValue.IsCloseTo(currentSize.Y, tolerance: defaultTolerance);

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
                            var isResizerXAligned =
                                lastLogicalValue.IsCloseTo(currentSize.X, tolerance: defaultTolerance);

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

            Disposable
                .Create(this, unregisterFunc.Invoke)
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

    internal void SwapPanels(PanelId firstPanelId, PanelId secondPanelId)
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

            updater.AddRange(newStates.Select(state =>
            {
                var image = IconUtils.StateToBitmap(state);
                return new AddPanelButtonViewModel(state, image);
            }));
        });
    }

    private void UpdateResizers()
    {
        _resizersSource.Edit(updater =>
        {
            updater.Clear();

            var currentState = WorkspaceGridState.From(_panels, IsHorizontal);
            var resizers = GridUtils.GetResizers(currentState);

            updater.AddRange(resizers.Select(info =>
            {
                var vm = new PanelResizerViewModel(info.Start, info.End, info.IsHorizontal, info.ConnectedPanels);
                vm.Arrange(_lastWorkspaceSize);
                return vm;
            }));
        });
    }

    internal void AddPanel(WorkspaceGridState newWorkspaceState, AddPanelBehavior behavior)
    {
        behavior.Switch(
            f0: _ => AddPanelWithDefaultTab(newWorkspaceState),
            f1: withCustomTab => AddPanelWithCustomTab(newWorkspaceState, withCustomTab.PageData)
        );
    }

    private PageData GetDefaultPageData()
    {
        var allDetails = _factoryController.GetAllDetails(Context).ToArray();
        var pageData = new PageData
        {
            FactoryId = NewTabPageFactory.StaticId,
            Context = new NewTabPageContext
            {
                DiscoveryDetails = allDetails
            }
        };

        return pageData;
    }

    internal void OpenPage(Optional<PageData> optionalPageData, OpenPageBehavior behavior, bool selectTab = true)
    {
        var pageData = optionalPageData.ValueOr(GetDefaultPageData);

        behavior.Switch(
            f0: replaceTab => OpenPageReplaceTab(pageData, replaceTab, selectTab),
            f1: newTab => OpenPageInNewTab(pageData, newTab),
            f2: newPanel => OpenPageInNewPanel(pageData, newPanel),
            f3: _ => OpenPagePrimaryDefault(pageData, selectTab),
            f4: _ => OpenPageSecondaryDefault(pageData, selectTab)
        );
    }



    private void OpenPageReplaceTab(PageData pageData, OpenPageBehavior.ReplaceTab replaceTab, bool selectTab)
    {
        var panel = OptionalPanelOrFirst(replaceTab.PanelId);
        var tab = OptionalTabOrFirst(panel, replaceTab.TabId);

        // Check if the page is already open in the tab
        var tabData = tab.ToData();
        if (tabData.PageData.FactoryId == pageData.FactoryId && tabData.PageData.Context == pageData.Context)
        {
            if (selectTab) panel.SelectTab(tab.Id);
            return;
        }

        // Replace the tab contents
        var newTabPage = _factoryController.Create(pageData, WindowId, Id, panel.Id, tab.Id);
        tab.Contents = newTabPage;

        if (selectTab) panel.SelectTab(tab.Id);
        panel.SelectTab(tab.Id);
    }

    private void OpenPageInNewTab(PageData pageData, OpenPageBehavior.NewTab newTab)
    {
        var panel = OptionalPanelOrFirst(newTab.PanelId);
        panel.AddCustomTab(pageData);
    }

    private void OpenPagePrimaryDefault(PageData pageData, bool selectTab)
    {
        // TODO: Query the PageData for default behavior for the particular page
        // TODO: Query user settings for default behavior

        // Current primary default behavior is to replace the first tab if it's different
        ReplaceFirstTabIfDifferent(pageData, selectTab);
    }

    private void OpenPageSecondaryDefault(PageData pageData, bool selectTab)
    {
        // TODO: Query the PageData for default behavior for the particular page
        // TODO: Query user settings for default behavior

        // Current secondary default behavior is to open the page in a new tab in the first panel
        OpenPageInNewTab(pageData, new OpenPageBehavior.NewTab(Optional<PanelId>.None));
    }

    private void ReplaceFirstTabIfDifferent(PageData pageData, bool selectTab)
    {
        var panel = _panels.First();
        var tab = panel.Tabs.First();

        if (tab.ToData().PageData.FactoryId == pageData.FactoryId)
        {
            if (selectTab) panel.SelectTab(tab.Id);
            return;
        }

        var newTabPage = _factoryController.Create(pageData, WindowId, Id, panel.Id, tab.Id);
        tab.Contents = newTabPage;

        if (selectTab) panel.SelectTab(tab.Id);
    }

    private static IPanelTabViewModel OptionalTabOrFirst(IPanelViewModel panel, Optional<PanelTabId> optionalTabId)
    {
        if (optionalTabId.HasValue)
        {
            var optionalTab = panel.Tabs.FirstOrOptional(x => x.Id == optionalTabId.Value);
            if (optionalTab.HasValue) return optionalTab.Value;
        }

        return panel.Tabs.First();
    }

    private IPanelViewModel OptionalPanelOrFirst(Optional<PanelId> optionalPanelId)
    {
        if (optionalPanelId.HasValue)
        {
            if (_panelSource.Lookup(optionalPanelId.Value).TryGet(out var panel)) return panel;
        }

        return _panels.First();
    }

    private void OpenPageInNewPanel(PageData pageData, OpenPageBehavior.NewPanel newPanel)
    {
        var optionalNewWorkspaceState = newPanel.NewWorkspaceState;
        var newWorkspaceState = optionalNewWorkspaceState.ValueOr(() =>
        {
            var currentState = WorkspaceGridState.From(_panels, IsHorizontal);
            var newStates = GridUtils.GetPossibleStates(currentState, MaxColumns, MaxRows);
            return newStates.First();
        });

        AddPanelWithCustomTab(newWorkspaceState, pageData);
    }

    private void AddPanelWithDefaultTab(WorkspaceGridState newWorkspaceState)
    {
        AddPanelWithCustomTab(newWorkspaceState, GetDefaultPageData());
    }

    private void AddPanelWithCustomTab(WorkspaceGridState newWorkspaceState, PageData pageData)
    {
        _panelSource.Edit(updater =>
        {
            foreach (var panel in newWorkspaceState)
            {
                var (panelId, logicalBounds) = panel;
                if (panelId == PanelId.DefaultValue)
                {
                    var panelViewModel = new PanelViewModel(_workspaceController, _factoryController)
                    {
                        WindowId = WindowId,
                        WorkspaceId = Id,
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

    internal void ClosePanel(PanelId panelToClose)
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
                var vm = new PanelViewModel(_workspaceController, _factoryController)
                {
                    WindowId = WindowId,
                    WorkspaceId = Id
                };

                vm.FromData(panel);
                updater.AddOrUpdate(vm);
            }
        });
    }
}
