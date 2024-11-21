using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using Avalonia.Controls.Models.TreeDataGrid;
using DynamicData;
using DynamicData.Binding;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public class LoadOrderViewModel : AViewModel<ILoadOrderViewModel>, ILoadOrderViewModel
{
    private readonly ReadOnlyObservableCollection<ISortableItemViewModel> _sortableItemViewModels;

    public string SortOrderName { get; }
    public ReadOnlyObservableCollection<ISortableItemViewModel> SortableItems => _sortableItemViewModels;


    public LoadOrderTreeDataGridAdapter Adapter { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        SortOrderName = sortableItemProviderFactory.SortOrderName;
        var provider = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);

        var subscription = provider
            .SortableItems
            .ToObservableChangeSet()
            .Transform(item => (ISortableItemViewModel)new SortableItemViewModel(item))
            .Bind(out _sortableItemViewModels);

        Adapter = new LoadOrderTreeDataGridAdapter(provider);
        Adapter.ViewHierarchical.Value = true;

        this.WhenActivated(d =>
            {
                Adapter.Activate();
                Disposable.Create(() => Adapter.Deactivate())
                    .DisposeWith(d);

                subscription.Subscribe()
                    .DisposeWith(d);
            }
        );
    }
}

public class LoadOrderTreeDataGridAdapter : TreeDataGridAdapter<ILoadOrderItemModel, Guid>
{
    private ILoadoutSortableItemProvider _sortableItemsProvider;

    public LoadOrderTreeDataGridAdapter(ILoadoutSortableItemProvider sortableItemsProvider)
    {
        _sortableItemsProvider = sortableItemsProvider;
    }

    protected override IObservable<IChangeSet<ILoadOrderItemModel, Guid>> GetRootsObservable(bool viewHierarchical)
    {
        return _sortableItemsProvider.SortableItems
            .ToObservableChangeSet(item => item.ItemId)
            .Transform(item => (ILoadOrderItemModel)new LoadOrderItemModel(item));
    }

    protected override IColumn<ILoadOrderItemModel>[] CreateColumns(bool viewHierarchical)
    {
        return
        [
            // TODO: Use <see cref="ColumnCreator"/> to create the columns using interfaces
            new HierarchicalExpanderColumn<ILoadOrderItemModel>(
            inner: CreateIndexColumn(),
            childSelector: static model => model.Children,
            hasChildrenSelector: static model => model.HasChildren.Value,
            isExpandedSelector: static model => model.IsExpanded
            )
            {
            Tag = "expander",
            },
            CreateNameColumn(),
        ];
    }

    private static IColumn<ILoadOrderItemModel> CreateIndexColumn()
    {
        return new CustomTemplateColumn<ILoadOrderItemModel>(
            header: "Load Order",
            cellTemplateResourceKey: "LoadOrderItemIndexColumnTemplate",
            options: new TemplateColumnOptions<ILoadOrderItemModel>
            {
                CanUserSortColumn = false,
                CanUserResizeColumn = true,
            }
        )
        {
            Id = "Index",
        };
    }

    private static IColumn<ILoadOrderItemModel> CreateNameColumn()
    {
        return new CustomTemplateColumn<ILoadOrderItemModel>(
            header: "Name",
            cellTemplateResourceKey: "LoadOrderItemNameColumnTemplate",
            options: new TemplateColumnOptions<ILoadOrderItemModel>
            {
                CanUserSortColumn = false,
                CanUserResizeColumn = true,
            }
        )
        {
            Id = "Name",
        };
    }
}
