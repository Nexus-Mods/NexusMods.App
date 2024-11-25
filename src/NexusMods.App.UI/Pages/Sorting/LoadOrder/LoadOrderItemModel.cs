using System.Reactive;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderItemModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    public ISortableItem InnerItem { get; }
    
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }
    
    // TODO: Populate these properly
    public string ModName { get; }
    public bool IsActive { get; }

    public LoadOrderItemModel(ISortableItem sortableItem)
    {
        InnerItem = sortableItem;
        SortIndex = sortableItem.SortIndex;
        DisplayName = sortableItem.DisplayName;
        
        // TODO: Should this be a subscription?
        IsActive = sortableItem.IsActive;
        ModName = sortableItem.ModName;
        
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
