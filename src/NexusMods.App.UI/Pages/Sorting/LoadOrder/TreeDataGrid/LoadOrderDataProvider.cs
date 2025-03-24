using System.ComponentModel;
using System.Reactive.Linq;
using DynamicData;
using Humanizer;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using R3;
using ReactiveUI;
using static NexusMods.App.UI.Pages.Sorting.LoadOrderComponents;
// ReSharper disable InvokeAsExtensionMethod

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDataProvider : ILoadOrderDataProvider
{
    
    public IObservable<IChangeSet<CompositeItemModel<Guid>, Guid>> ObserveLoadOrder(
        ILoadoutSortableItemProvider sortableItemProvider,
        Observable<ListSortDirection> sortDirectionObservable)
    {
        var lastIndexObservable = sortableItemProvider.SortableItemsChangeSet
            .QueryWhenChanged(query => query.Count == 0 ? 0 : query.Items.Max(item => item.SortIndex))
            .ToObservable()
            .Prepend(sortableItemProvider.SortableItems.Count == 0 ? 0 : sortableItemProvider.SortableItems.Max(item => item.SortIndex));

        var topMostIndexObservable = R3.Observable.CombineLatest(
                sortDirectionObservable,
                lastIndexObservable,
                (sortDirection, lastIndex) => sortDirection == ListSortDirection.Ascending ? 0 : lastIndex
            )
            .Publish()
            .RefCount();

        var bottomMostIndexObservable = R3.Observable.CombineLatest(
                sortDirectionObservable,
                lastIndexObservable,
                (sortDirection, lastIndex) => sortDirection == ListSortDirection.Ascending ? lastIndex : 0
            )
            .Publish()
            .RefCount();
        
        return sortableItemProvider.SortableItemsChangeSet
            .Transform( item => ToLoadOrderItemModel(item, topMostIndexObservable, bottomMostIndexObservable));
    }


    private static CompositeItemModel<Guid> ToLoadOrderItemModel(
        ISortableItem sortableItem,
        R3.Observable<int> topMostIndexObservable,
        R3.Observable<int> bottomMostIndexObservable)
    {
        var compositeModel = new CompositeItemModel<Guid>(sortableItem.ItemId);

        // DisplayName
        compositeModel.Add(LoadOrderColumns.NameColumn.NameComponentKey,
            new StringComponent(sortableItem.DisplayName, sortableItem.WhenAnyValue(item => item.DisplayName)));

        // ModName
        compositeModel.Add(LoadOrderColumns.NameColumn.ModNameComponentKey,
            new StringComponent(sortableItem.ModName, sortableItem.WhenAnyValue(item => item.ModName)));

        // IsActive
        compositeModel.Add(LoadOrderColumns.IsActiveComponentKey,
            new ValueComponent<bool>(sortableItem.IsActive, sortableItem.WhenAnyValue(item => item.IsActive)));

        // SortIndex
        var sortIndexObservable = sortableItem.WhenAnyValue(item => item.SortIndex).ToObservable();
        
        var canExecuteMoveUp = R3.Observable.CombineLatest(
            sortIndexObservable,
            topMostIndexObservable,
            (sortIndex, topMostIndex) => sortIndex != topMostIndex
        );
        var canExecuteMoveDown = R3.Observable.CombineLatest(
            sortIndexObservable,
            bottomMostIndexObservable,
            (sortIndex, bottomMost) => sortIndex != bottomMost
        );
        
        var displayIndexObservable = sortableItem.WhenAnyValue(item => item.SortIndex).Select(index => index.Ordinalize());
        
        compositeModel.Add(LoadOrderColumns.IndexColumn.IndexComponentKey,
            new IndexComponent(
                new ValueComponent<int>(sortableItem.SortIndex, sortIndexObservable),
                new ValueComponent<string>(sortableItem.SortIndex.Ordinalize(), displayIndexObservable),
                canExecuteMoveUp,
                canExecuteMoveDown
            )
        );

        
        return compositeModel;
    }
}
