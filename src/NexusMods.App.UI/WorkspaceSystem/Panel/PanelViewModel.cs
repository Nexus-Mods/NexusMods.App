using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using NexusMods.App.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; } = PanelId.New();

    private readonly SourceCache<IPanelTabViewModel, PanelTabId> _tabsSource = new(x => x.Id);
    private readonly ReadOnlyObservableCollection<IPanelTabViewModel> _tabs;
    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => _tabs;

    /// <inheritdoc/>
    [Reactive] public Rect LogicalBounds { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect ActualBounds { get; private set; }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<Unit, PanelId> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> PopoutCommand { get; }

    private readonly PageFactoryController _factoryController;
    public PanelViewModel(PageFactoryController factoryController)
    {
        _factoryController = factoryController;

        PopoutCommand = Initializers.DisabledReactiveCommand;
        CloseCommand = ReactiveCommand.Create(() => Id);

        AddTabCommand = ReactiveCommand.Create(() =>
        {
            AddTab();
        });
        _tabsSource
            .Connect()
            .DisposeMany()
            .Sort(PanelTabComparer.Instance)
            .Bind(out _tabs)
            .OnItemRemoved(removed =>
            {
                var removedIndex = removed.Index.Value;

                // set new selected tab
                if (removed.IsVisible)
                {
                    if (_tabs.Count != 0)
                    {
                        // if (removedIndex >= _tabs.Count - 1)
                        //     SelectedTabId = _tabs[^1].Id;
                        // else if (removedIndex == 0)
                        //     SelectedTabId = _tabs[0].Id;
                        // else
                        //     SelectedTabId = _tabs[(int)removedIndex].Id;
                    }
                }

                // update indices
                for (var i = removedIndex; i < _tabs.Count; i++)
                {
                    var next = _tabs[(int)i];
                    next.Index = PanelTabIndex.From(i);
                }
            })
            .Subscribe();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);

            // handle the close command on tabs
            _tabsSource
                .Connect()
                .MergeMany(item => item.Header.CloseTabCommand)
                .Subscribe(CloseTab)
                .DisposeWith(disposables);

            // handle when a tab gets selected
            _tabsSource
                .Connect()
                .WhenPropertyChanged(item => item.Header.IsSelected)
                .Where(propertyValue => propertyValue.Value)
                .Select(propertyValue => propertyValue.Sender)
                .Subscribe(selectedTab =>
                {
                    selectedTab.IsVisible = true;

                    foreach (var otherTab in Tabs)
                    {
                        if (otherTab.Id == selectedTab.Id) continue;
                        otherTab.IsVisible = false;
                        otherTab.Header.IsSelected = false;
                    }
                })
                .DisposeWith(disposables);

            // _tabsSource
            //     .Connect()
            //     .DisposeMany()
            //     .Sort(PanelTabComparer.Instance)
            //     .Bind(out _tabs)
            //     .Do(changeSet =>
            //     {
            //         if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Add, out var added))
            //         {
            //             SelectedTabId = added.Key;
            //         }
            //
            //         if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Remove, out var removed))
            //         {
            //             if (_tabs.Count == 0)
            //             {
            //                 CloseCommand.Execute().Subscribe();
            //                 return;
            //             }
            //
            //             var removedIndex = removed.Current.Index.Value;
            //
            //             // set new selected tab
            //             if (SelectedTabId == removed.Key)
            //             {
            //                 if (_tabs.Count != 0)
            //                 {
            //                     if (removedIndex >= _tabs.Count - 1)
            //                         SelectedTabId = _tabs[^1].Id;
            //                     else if (removedIndex == 0)
            //                         SelectedTabId = _tabs[0].Id;
            //                     else
            //                         SelectedTabId = _tabs[(int)removedIndex].Id;
            //                 }
            //                 else
            //                 {
            //                     SelectedTabId = PanelTabId.Empty;
            //                 }
            //             }
            //
            //             // update indices
            //             for (var i = removedIndex; i < _tabs.Count; i++)
            //             {
            //                 var next = _tabs[(int)i];
            //                 next.Index = PanelTabIndex.From(i);
            //             }
            //         }
            //     })
            //     .Transform(tab => tab.Header)
            //     .Bind(out _tabHeaders)
            //     .SubscribeWithErrorLogging()
            //     .DisposeWith(disposables);
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

    public IPanelTabViewModel AddTab()
    {
        var nextIndex = _tabs.Count == 0
            ? PanelTabIndex.From(0)
            : PanelTabIndex.From(_tabs.Last().Index.Value + 1);

        var tab = new PanelTabViewModel(nextIndex)
        {
            // TODO: show "new page tab"
            Contents = _factoryController.Create(new PageData
            {
                FactoryId = DummyPageFactory.Id,
                Context = new DummyPageContext(),
            })
        };

        _tabsSource.AddOrUpdate(tab);
        return tab;
    }

    public void CloseTab(PanelTabId id)
    {
        _tabsSource.Remove(id);
    }

    public PanelData ToData()
    {
        // TODO:
        // var selectedTab = _tabsSource.Lookup(SelectedTabId);
        // var selectedTabIndex = selectedTab.HasValue ? selectedTab.Value.Index : PanelTabIndex.Max;

        return new PanelData
        {
            LogicalBounds = LogicalBounds,
            Tabs = _tabs.Select(tab => tab.ToData()).ToArray(),
            SelectedTabIndex = PanelTabIndex.Max
        };
    }

    public void FromData(PanelData data)
    {
        LogicalBounds = data.LogicalBounds;

        _tabsSource.Clear();
        _tabsSource.Edit(updater =>
        {
            for (uint i = 0; i < data.Tabs.Length; i++)
            {
                var tab = data.Tabs[i];
                var index = PanelTabIndex.From(i);
                var vm = new PanelTabViewModel(index)
                {
                    Contents = _factoryController.Create(tab.PageData)
                };

                updater.AddOrUpdate(vm);
            }
        });
    }
}
