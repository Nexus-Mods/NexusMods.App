using System.ComponentModel;
using System.Reactive;
using System.Reactive.Disposables;
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
    
    // TODO: Populate these properly
    public string InfoAlertTitle { get; } = "";
    public string InfoAlertHeading { get; } = "";
    public string InfoAlertMessage { get; } = "";
    [Reactive] public bool InfoAlertIsVisible { get; set; } = false;
    public ReactiveCommand<Unit, Unit> InfoAlertCommand { get; } = ReactiveCommand.Create(() => { });
    public string TrophyToolTip { get; } = "";
    [Reactive] public ListSortDirection SortDirectionCurrent { get; set; } = ListSortDirection.Ascending;

    public LoadOrderTreeDataGridAdapter Adapter { get; }

    public LoadOrderViewModel(LoadoutId loadoutId, ISortableItemProviderFactory sortableItemProviderFactory)
    {
        SortOrderName = sortableItemProviderFactory.SortOrderName;
        var provider = sortableItemProviderFactory.GetLoadoutSortableItemProvider(loadoutId);

        Adapter = new LoadOrderTreeDataGridAdapter(provider);
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
