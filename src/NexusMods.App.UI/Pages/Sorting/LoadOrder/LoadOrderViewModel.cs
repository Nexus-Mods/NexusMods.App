using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using R3;
using NexusMods.App.UI.Controls.Alerts;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using CompositeDisposable = System.Reactive.Disposables.CompositeDisposable;
using Disposable = System.Reactive.Disposables.Disposable;
using ReactiveCommand = ReactiveUI.ReactiveCommand;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public string SortOrderName { get; }
    public string InfoAlertTitle { get; }
    public string InfoAlertHeading { get; }
    public string InfoAlertMessage { get; }
    [Reactive] public bool InfoAlertIsVisible { get; set; }
    public ReactiveUI.ReactiveCommand<Unit, Unit> InfoAlertCommand { get; }
    public string TrophyToolTip { get; }
    [Reactive] public ListSortDirection SortDirectionCurrent { get; set; }
    [Reactive] public bool IsWinnerTop { get; private set; }
    public string EmptyStateMessageTitle { get; }
    public string EmptyStateMessageContents { get; }

    public AlertSettingsWrapper AlertSettingsWrapper { get; }

    public TreeDataGridAdapter<ILoadOrderItemModel, Guid> Adapter { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory itemProviderFactory, ISettingsManager settingsManager)
    {
        var provider = itemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);

        SortOrderName = itemProviderFactory.SortOrderName;
        InfoAlertTitle = itemProviderFactory.OverrideInfoTitle;
        InfoAlertHeading = itemProviderFactory.OverrideInfoHeading;
        InfoAlertMessage = itemProviderFactory.OverrideInfoMessage;
        TrophyToolTip = itemProviderFactory.WinnerIndexToolTip;
        EmptyStateMessageTitle = itemProviderFactory.EmptyStateMessageTitle;
        EmptyStateMessageContents = itemProviderFactory.EmptyStateMessageContents;

        // TODO: load these from settings
        SortDirectionCurrent = itemProviderFactory.SortDirectionDefault;
        InfoAlertIsVisible = true;

        IsWinnerTop = SortDirectionCurrent == ListSortDirection.Ascending &&
                      itemProviderFactory.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;

        var sortDirectionObservable = this.WhenAnyValue(vm => vm.SortDirectionCurrent)
            .Publish(SortDirectionCurrent);

        var lastIndexObservable = provider.SortableItems
            .ToObservableChangeSet(item => item.ItemId)
            .Maximum(item => item.SortIndex)
            .Publish(provider.SortableItems.Count);

        var adapter = new LoadOrderTreeDataGridAdapter(provider, sortDirectionObservable, lastIndexObservable);
        Adapter = adapter;
        Adapter.ViewHierarchical.Value = true;

        AlertSettingsWrapper = new AlertSettingsWrapper(settingsManager, "cyberpunk2077 redmod load-order first-loaded-wins");

        InfoAlertCommand = ReactiveCommand.Create(() => { AlertSettingsWrapper.ShowAlert(); });

        this.WhenActivated(d =>
            {
                Adapter.Activate();
                Disposable.Create(() => Adapter.Deactivate())
                    .DisposeWith(d);

                sortDirectionObservable.Connect()
                    .DisposeWith(d);

                lastIndexObservable.Connect()
                    .DisposeWith(d);

                // Update IsWinnerTop
                sortDirectionObservable.Subscribe(sortDirection =>
                        {
                            var isAscending = sortDirection == ListSortDirection.Ascending;
                            IsWinnerTop = isAscending &&
                                          itemProviderFactory.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins;
                        }
                    )
                    .DisposeWith(d);

                // Move up/down commands
                adapter.MessageSubject
                    .SubscribeAwait(async (payload, cancellationToken) =>
                        {
                            var (item, delta) = payload;
                            await provider.SetRelativePosition(((LoadOrderItemModel)item).InnerItem, delta);
                        }
                    )
                    .DisposeWith(d);
            }
        );
    }
}

public readonly record struct MoveUpDownCommandPayload(ILoadOrderItemModel Item, int Delta);

public class LoadOrderTreeDataGridAdapter : TreeDataGridAdapter<ILoadOrderItemModel, Guid>,
    ITreeDataGirdMessageAdapter<MoveUpDownCommandPayload>
{
    private readonly ILoadoutSortableItemProvider _sortableItemsProvider;
    private readonly IObservable<ListSortDirection> _sortDirectionObservable;
    private readonly IObservable<int> _lastIndexObservable;
    private readonly CompositeDisposable _disposables = new();
    private readonly IObservable<ISortedChangeSet<ILoadOrderItemModel, Guid>> _sortedItems;

    public Subject<MoveUpDownCommandPayload> MessageSubject { get; } = new();

    public LoadOrderTreeDataGridAdapter(
        ILoadoutSortableItemProvider sortableItemsProvider,
        IObservable<ListSortDirection> sortDirectionObservable,
        IObservable<int> lastIndexObservable)
    {
        _sortableItemsProvider = sortableItemsProvider;
        _sortDirectionObservable = sortDirectionObservable;
        _lastIndexObservable = lastIndexObservable;

        var itemsChangeSet = _sortableItemsProvider.SortableItems
            .ToObservableChangeSet(item => item.ItemId)
            .Transform(ILoadOrderItemModel (item) => new LoadOrderItemModel(
                    item,
                    _sortDirectionObservable,
                    _lastIndexObservable,
                    MessageSubject
                )
            );
        
        var ascendingComparer = SortExpressionComparer<ILoadOrderItemModel>.Ascending(item => item.SortIndex);
        var descendingComparer = SortExpressionComparer<ILoadOrderItemModel>.Descending(item => item.SortIndex);
        var comparerObservable = _sortDirectionObservable.Select(sortDirection =>
            {
                return sortDirection == ListSortDirection.Ascending
                    ? ascendingComparer
                    : descendingComparer;
            }
        );
        
        _sortedItems = itemsChangeSet.Sort(comparerObservable);
    }

    protected override IObservable<IChangeSet<ILoadOrderItemModel, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        return _sortedItems;
    }

    protected override IColumn<ILoadOrderItemModel>[] CreateColumns(bool viewHierarchical)
    {
        return
        [
            // TODO: Use <see cref="ColumnCreator"/> to create the columns using interfaces
            new HierarchicalExpanderColumn<ILoadOrderItemModel>(
                inner: CreateIndexColumn(_sortableItemsProvider.ParentFactory.IndexColumnHeader),
                childSelector: static model => model.Children,
                hasChildrenSelector: static model => model.HasChildren.Value,
                isExpandedSelector: static model => model.IsExpanded
            )
            {
                Tag = "expander",
            },
            CreateNameColumn(_sortableItemsProvider.ParentFactory.NameColumnHeader),
        ];
    }

    internal static IColumn<ILoadOrderItemModel> CreateIndexColumn(string headerName)
    {
        return new CustomTemplateColumn<ILoadOrderItemModel>(
            header: headerName,
            cellTemplateResourceKey: "LoadOrderItemIndexColumnTemplate",
            options: new TemplateColumnOptions<ILoadOrderItemModel>
            {
                CanUserSortColumn = false,
                CanUserResizeColumn = false,
            }
        )
        {
            Id = "Index",
        };
    }

    internal static IColumn<ILoadOrderItemModel> CreateNameColumn(string headerName)
    {
        return new CustomTemplateColumn<ILoadOrderItemModel>(
            header: headerName,
            cellTemplateResourceKey: "LoadOrderItemNameColumnTemplate",
            options: new TemplateColumnOptions<ILoadOrderItemModel>
            {
                CanUserSortColumn = false,
                CanUserResizeColumn = false,
            }
        )
        {
            Id = "Name",
        };
    }

    private bool _isDisposed;

    protected override void Dispose(bool disposing)
    {
        if (!_isDisposed)
        {
            if (disposing)
            {
                R3.Disposable.Dispose(_disposables, MessageSubject);
            }

            _isDisposed = true;
        }

        base.Dispose(disposing);
    }
}
