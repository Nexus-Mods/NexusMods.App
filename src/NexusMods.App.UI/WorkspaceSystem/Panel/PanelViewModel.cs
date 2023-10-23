using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using DynamicData;
using NexusMods.App.UI.Controls;
using NexusMods.Common;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.WorkspaceSystem;

public class PanelViewModel : AViewModel<IPanelViewModel>, IPanelViewModel
{
    public PanelId Id { get; private set; } = PanelId.New();

    private readonly SourceCache<IPanelTabViewModel, PanelTabId> _tabsSource = new(x => x.Id);

    private ReadOnlyObservableCollection<IPanelTabViewModel> _tabs = Initializers.ReadOnlyObservableCollection<IPanelTabViewModel>();
    public ReadOnlyObservableCollection<IPanelTabViewModel> Tabs => _tabs;

    private ReadOnlyObservableCollection<IPanelTabHeaderViewModel> _tabHeaders = Initializers.ReadOnlyObservableCollection<IPanelTabHeaderViewModel>();
    public ReadOnlyObservableCollection<IPanelTabHeaderViewModel> TabHeaders => _tabHeaders;

    [Reactive]
    public PanelTabId SelectedTabId { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect LogicalBounds { get; set; }

    /// <inheritdoc/>
    [Reactive] public Rect ActualBounds { get; private set; }

    public ReactiveCommand<Unit, Unit> AddTabCommand { get; }
    public ReactiveCommand<Unit, Unit> CloseCommand { get; }
    public ReactiveCommand<Unit, Unit> PopoutCommand { get; }

    private readonly IWorkspaceViewModel _workspaceViewModel;
    public PanelViewModel(IWorkspaceViewModel workspaceViewModel)
    {
        _workspaceViewModel = workspaceViewModel;

        // TODO: open in new window
        PopoutCommand = Initializers.DisabledReactiveCommand;
        CloseCommand = ReactiveCommand.Create(ClosePanel);

        AddTabCommand = ReactiveCommand.Create(() =>
        {
            AddTab();
        });

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(vm => vm.LogicalBounds)
                .SubscribeWithErrorLogging(_ => UpdateActualBounds())
                .DisposeWith(disposables);

            _tabsSource
                .Connect()
                .DisposeMany()
                .Sort(PanelTabComparer.Instance)
                .Bind(out _tabs)
                .Do(changeSet =>
                {
                    if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Add, out var added))
                    {
                        SelectedTabId = added.Key;
                    }

                    if (changeSet.TryGetFirst(change => change.Reason == ChangeReason.Remove, out var removed))
                    {
                        if (_tabs.Count == 0)
                        {
                            ClosePanel();
                            return;
                        }

                        var removedIndex = removed.Current.Index.Value;

                        // set new selected tab
                        if (SelectedTabId == removed.Key)
                        {
                            if (_tabs.Count != 0)
                            {
                                if (removedIndex >= _tabs.Count - 1)
                                    SelectedTabId = _tabs[^1].Id;
                                else if (removedIndex == 0)
                                    SelectedTabId = _tabs[0].Id;
                                else
                                    SelectedTabId = _tabs[(int)removedIndex].Id;
                            }
                            else
                            {
                                SelectedTabId = PanelTabId.Empty;
                            }
                        }

                        // update indices
                        for (var i = removedIndex; i < _tabs.Count; i++)
                        {
                            var next = _tabs[(int)i];
                            next.Index = PanelTabIndex.From(i);
                        }
                    }
                })
                .Transform(tab => tab.Header)
                .Bind(out _tabHeaders)
                .SubscribeWithErrorLogging()
                .DisposeWith(disposables);

            this.WhenAnyValue(vm => vm.SelectedTabId)
                .Select(tabId => _tabsSource.Lookup(tabId))
                .SubscribeWithErrorLogging(optional =>
                {
                    var tab = optional.HasValue ? optional.Value : null;

                    if (tab is not null)
                    {
                        tab.Header.IsSelected = true;
                        tab.IsVisible = true;
                    }

                    foreach (var tabViewModel in _tabs)
                    {
                        tabViewModel.Header.IsSelected = ReferenceEquals(tabViewModel, tab);
                        tabViewModel.IsVisible = ReferenceEquals(tabViewModel, tab);
                    }
                })
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

    public IPanelTabViewModel AddTab()
    {
        var nextIndex = _tabs.Count == 0
            ? PanelTabIndex.From(0)
            : PanelTabIndex.From(_tabs.Last().Index.Value + 1);

        var tab = new PanelTabViewModel(this, nextIndex)
        {
            // TODO:
            Contents = new DummyViewModel()
        };

        _tabsSource.AddOrUpdate(tab);
        return tab;
    }

    public void CloseTab(PanelTabId id)
    {
        _tabsSource.Remove(id);
    }

    private void ClosePanel()
    {
        _workspaceViewModel.ClosePanel(this);
        _tabsSource.Clear();
        _tabsSource.Dispose();
    }

    public PanelData ToData()
    {
        return new PanelData
        {
            Id = Id,
            LogicalBounds = LogicalBounds,
            Tabs = _tabs.Select(tab => tab.ToData()).ToArray()
        };
    }

    public void FromData(PanelData data)
    {
        Id = data.Id;
        LogicalBounds = data.LogicalBounds;

        _tabsSource.Clear();

        _tabsSource.Edit(updater =>
        {
            for (uint i = 0; i < data.Tabs.Length; i++)
            {
                var index = PanelTabIndex.From(i);
                var vm = new PanelTabViewModel(this, index);

                // TODO:
                vm.Contents = new DummyViewModel();

                updater.AddOrUpdate(vm);
            }
        });
    }
}
