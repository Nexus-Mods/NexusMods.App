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
    public string ModName { get; } = string.Empty;
    public bool IsActive { get; } = true;

    public LoadOrderItemModel(ISortableItem sortableItem)
    {
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
