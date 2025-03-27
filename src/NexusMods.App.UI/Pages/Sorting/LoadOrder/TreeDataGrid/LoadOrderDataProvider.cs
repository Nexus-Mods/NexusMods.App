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
            .Replay(1)
            .RefCount();

        var bottomMostIndexObservable = R3.Observable.CombineLatest(
                sortDirectionObservable,
                lastIndexObservable,
                (sortDirection, lastIndex) => sortDirection == ListSortDirection.Ascending ? lastIndex : 0
            )
            .Replay(1)
            .RefCount();;
        
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
        
        
        // The UI requires 1-based indexes, so we convert the 0-based index to a 1-based ordinalized string.
        var displayIndexObservable = sortableItem.WhenAnyValue(item => item.SortIndex).Select(ToOneBasedOrdinalized);
        
        compositeModel.Add(LoadOrderColumns.IndexColumn.IndexComponentKey,
            new IndexComponent(
                new ValueComponent<int>(sortableItem.SortIndex, sortIndexObservable),
                new ValueComponent<string>(ToOneBasedOrdinalized(sortableItem.SortIndex), displayIndexObservable),
                canExecuteMoveUp,
                canExecuteMoveDown
            )
        );

        return compositeModel;
    }
    
    /// <summary>
    /// Converts a 0-based index to a 1-based ordinalized string.
    /// </summary>
    /// <param name="index">The 0-based index.</param>
    /// <returns>A 1-based ordinalized string.</returns>
    private static string ToOneBasedOrdinalized(int index) => (index + 1).Ordinalize();
}
