using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using Humanizer;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using ReactiveUI;
using static NexusMods.App.UI.Pages.Sorting.LoadOrderComponents;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderDataProvider : ILoadOrderDataProvider
{
    
    public IObservable<IChangeSet<CompositeItemModel<Guid>, Guid>> ObserveLoadOrder(ILoadoutSortableItemProvider sortableItemProvider)
    {
        return sortableItemProvider.SortableItems
            .ToObservableChangeSet(item => item.ItemId)
            .Transform( item => ToLoadOrderItemModel(item));
    }


    private CompositeItemModel<Guid> ToLoadOrderItemModel(ISortableItem sortableItem)
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
        var displayIndexObservable = sortableItem.WhenAnyValue(item => item.SortIndex).Select(index => index.Ordinalize());
        compositeModel.Add(LoadOrderColumns.IndexColumn.IndexComponentKey,
            new IndexComponent(
                new ValueComponent<int>(sortableItem.SortIndex, sortableItem.WhenAnyValue(item => item.SortIndex)),
                new ValueComponent<string>(sortableItem.SortIndex.Ordinalize(), displayIndexObservable)
            )
        );

        return compositeModel;
    }
}
