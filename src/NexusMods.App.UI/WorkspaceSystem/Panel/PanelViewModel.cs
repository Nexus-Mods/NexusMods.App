using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Kernel;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Windows;
using NexusMods.Extensions.BCL;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    /// <inheritdoc/>
    public PanelId Id { get; } = PanelId.NewId();

    /// <inheritdoc/>
    public required WindowId WindowId { get; set; }

    /// <inheritdoc/>
    public required WorkspaceId WorkspaceId { get; set; }

    private readonly SourceList<IPanelTabViewModel> _tabsList = new();
    private readonly ReadOnlyObservableCollection<IPanelTabViewModel> _tabs;
    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => _tabs;

    /// <inheritdoc/>
    [Reactive] public Rect LogicalBounds { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect ActualBounds { get; private set; }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<Unit, PanelId> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> PopoutCommand { get; }

    [Reactive]
    public bool IsNotAlone { get; set; }

    [Reactive] private PanelTabId SelectedTabId { get; set; }

    private readonly IWorkspaceController _workspaceController;
    private readonly PageFactoryController _factoryController;

    public PanelViewModel(IWorkspaceController workspaceController, PageFactoryController factoryController)
    {
        _workspaceController = workspaceController;
        _factoryController = factoryController;

        var canExecute = this.WhenAnyValue(vm => vm.IsNotAlone);
        PopoutCommand = ReactiveCommand.Create(() => { }, canExecute);
        CloseCommand = ReactiveCommand.Create(() => Id, canExecute);

        AddTabCommand = ReactiveCommand.Create(AddDefaultTab);

        _tabsList
            .Connect()
            .Bind(out _tabs)
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);

            // close the panel when all tabs are closed
            _tabsList
                .Connect()
                .Count()
                .Where(count => count == 0)
                .Select(_ => Unit.Default)
                .InvokeCommand(CloseCommand)
                .DisposeWith(disposables);

            // change the header when the panel only has a single tab vs multiple tabs
            _tabsList
                .Connect()
                .Count()
                .Select(count => count == 1)
                .Do(hasOneTab =>
                {
                    foreach (var tab in _tabs)
                    {
                        tab.Header.CanClose = !hasOneTab;
                    }
                })
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            // handle when a tab gets removed
            _tabsList
                .Connect()
                .ForEachItemChange(itemChange =>
                {
                    if (itemChange.Reason != ListChangeReason.Remove) return;
                    if (!itemChange.Current.IsVisible) return;
                    if (Tabs.Count == 0) return;

                    // select a new tab
                    var removedIndex = itemChange.CurrentIndex;
                    if (removedIndex >= Tabs.Count - 1)
                        SelectedTabId = Tabs[^1].Id;
                    else if (removedIndex == 0)
                        SelectedTabId = Tabs[0].Id;
                    else
                        SelectedTabId = Tabs[removedIndex].Id;
                })
                .Subscribe()
                .DisposeWith(disposables);

            // handle the close command on tabs
            _tabsList
                .Connect()
                .MergeMany(item => item.Header.CloseTabCommand)
                .Subscribe(CloseTab)
                .DisposeWith(disposables);

            // handle when a tab gets selected
            // 1) set SelectedTabId
            _tabsList
                .Connect()
                .WhenPropertyChanged(item => item.Header.IsSelected)
                .Where(propertyValue => propertyValue.Value)
                .Select(propertyValue => propertyValue.Sender.Id)
                // NOTE(erri120): this throws an exception, see #751
                // .BindToVM(this, vm => vm.SelectedTabId)
                .Subscribe(selectedTabId => SelectedTabId = selectedTabId)
                .DisposeWith(disposables);

            // 2) update the visibility of the tabs
            this.WhenAnyValue(vm => vm.SelectedTabId)
                .Do(selectedTabId =>
                {
                    foreach (var tab in Tabs)
                    {
                        tab.IsVisible = tab.Id == selectedTabId;
                        tab.Header.IsSelected = tab.Id == selectedTabId;
                    }
                })
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private Size _workspaceSize = MathUtils.Zero;
    private void UpdateActualBounds()
    {
        ActualBounds = MathUtils.CalculateActualBounds(_workspaceSize, LogicalBounds);
    }

    /// <inheritdoc/>
    public void Arrange(Size workspaceSize)
    {
        _workspaceSize = workspaceSize;
        UpdateActualBounds();
    }

    public void AddDefaultTab()
    {
        _workspaceController.OpenPage(WorkspaceId, Optional<PageData>.None, new OpenPageBehavior.NewTab(Id));
    }

    public void AddCustomTab(PageData pageData)
    {
        var newTabPage = _factoryController.Create(pageData, WindowId, WorkspaceId, Id, tabId: Optional<PanelTabId>.None);
        var tab = new PanelTabViewModel
        {
            Contents = newTabPage,
            Header =
            {
                Icon = newTabPage.ViewModel.TabIcon,
                Title = newTabPage.ViewModel.TabTitle,
            },
        };

        newTabPage.ViewModel.TabId = tab.Id;

        _tabsList.Edit(updater => updater.Add(tab));
        SelectedTabId = tab.Id;
    }

    public void SelectTab(PanelTabId tabId)
    {
        if (_tabsList.Items.Any(tab => tab.Id == tabId))
            SelectedTabId = tabId;
    }

    public void CloseTab(PanelTabId id)
    {
        _tabsList.Edit(updater =>
        {
            var index = updater.LinearSearch(item => item.Id == id);
            updater.RemoveAt(index);
        });
    }

    public PanelData ToData()
    {
        return new PanelData
        {
            LogicalBounds = LogicalBounds,
            Tabs = _tabs.Select(tab => tab.ToData()).NotNull().ToArray(),
            SelectedTabId = SelectedTabId,
        };
    }

    public void FromData(PanelData data)
    {
        LogicalBounds = data.LogicalBounds;

        _tabsList.Edit(updater =>
        {
            updater.Clear();
            for (uint i = 0; i < data.Tabs.Length; i++)
            {
                var tab = data.Tabs[i];
                var newTabPage = _factoryController.Create(tab.PageData, WindowId, WorkspaceId, Id, tabId: Optional<PanelTabId>.None);

                var vm = new PanelTabViewModel
                {
                    Contents = newTabPage,
                    Header =
                    {
                        Icon = newTabPage.ViewModel.TabIcon,
                        Title = newTabPage.ViewModel.TabTitle,
                    },
                };

                newTabPage.ViewModel.TabId = vm.Id;

                updater.Add(vm);
            }
        });

        if (data.Tabs.Length == 0)
        {
            AddDefaultTab();
        }
        else
        {
            SelectedTabId = data.SelectedTabId;
        }
    }
}
