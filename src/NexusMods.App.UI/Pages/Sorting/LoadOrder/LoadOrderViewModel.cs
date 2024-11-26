using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    public string SortOrderName { get; }
    public string InfoAlertTitle { get; }
    public string InfoAlertHeading { get; }
    public string InfoAlertMessage { get; }
    [Reactive] public bool InfoAlertIsVisible { get; set; }
    public ReactiveCommand<Unit, Unit> InfoAlertCommand { get; } = ReactiveCommand.Create(() => { });
    public string TrophyToolTip { get; }
    [Reactive] public ListSortDirection SortDirectionCurrent { get; set; }
    [Reactive] public bool IsWinnerTop { get; set; }
    public string EmptyStateMessageTitle { get; }
    public string EmptyStateMessageContents { get; }

    public TreeDataGridAdapter<ILoadOrderItemModel, Guid> Adapter { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory itemProviderFactory)
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
        IsWinnerTop = itemProviderFactory.IndexOverrideBehavior == IndexOverrideBehavior.SmallerIndexWins && 
                      SortDirectionCurrent == ListSortDirection.Ascending;
        InfoAlertIsVisible = true;
        
        
        var sortDirectionObservable = this.WhenAnyValue(vm => vm.SortDirectionCurrent);
        Adapter = new LoadOrderTreeDataGridAdapter(provider, sortDirectionObservable);
        Adapter.ViewHierarchical.Value = true;

        this.WhenActivated(d =>
            {
                Adapter.Activate();
                Disposable.Create(() => Adapter.Deactivate())
                    .DisposeWith(d);
                
            }
        );
    }
}

public class LoadOrderTreeDataGridAdapter : TreeDataGridAdapter<ILoadOrderItemModel, Guid>
{
    private ILoadoutSortableItemProvider _sortableItemsProvider;
    private IObservable<ListSortDirection> _sortDirectionObservable;

    public LoadOrderTreeDataGridAdapter(ILoadoutSortableItemProvider sortableItemsProvider, IObservable<ListSortDirection> sortDirectionObservable)
    {
        _sortableItemsProvider = sortableItemsProvider;
        _sortDirectionObservable = sortDirectionObservable;
    }

    protected override IObservable<IChangeSet<ILoadOrderItemModel, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        var sortableItems = _sortableItemsProvider.SortableItems
            .ToObservableChangeSet(item => item.ItemId);
        
        var ascendingSortableItems = sortableItems
            .ToSortedCollection(item => item.SortIndex, SortDirection.Ascending)
            .ToObservableChangeSet(item => item.ItemId);
        
        var descendingSortableItems = sortableItems
            .ToSortedCollection(item => item.SortIndex, SortDirection.Descending)
            .ToObservableChangeSet(item => item.ItemId);
        
        // Sort the items based on SortDirection
        var sortedItems = _sortDirectionObservable
            .Select(direction => direction == ListSortDirection.Ascending ? ascendingSortableItems : descendingSortableItems)
            .Switch()
            .Transform(ILoadOrderItemModel (item) => new LoadOrderItemModel(item));

        return sortedItems;
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
}
