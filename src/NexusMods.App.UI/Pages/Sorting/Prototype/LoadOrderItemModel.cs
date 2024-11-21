using System.Reactive;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public class LoadOrderItemModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    public ISortableItem InnerItem { get; }
    public Guid Id { get; }
    
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }

    public LoadOrderItemModel(ISortableItem sortableItem, Guid id)
    {
        InnerItem = sortableItem;
        Id = id;
        
        InnerItem = sortableItem;
        SortIndex = sortableItem.SortIndex;
        DisplayName = sortableItem.DisplayName;
        
        MoveUp = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(InnerItem, delta: 1);
                return Unit.Default;
            }
        );
        
        MoveDown = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(InnerItem, delta: -1);
                return Unit.Default;
            }
        );
        
    }
    

    
}
