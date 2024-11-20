using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.Abstractions.Games;

public class SortableItemViewModel : AViewModel< ISortableItemViewModel>, ISortableItemViewModel
{
    public SortableItemViewModel(ISortableItem sortableItem)
    {
        SortableItem = sortableItem;
        SortIndex = sortableItem.SortIndex;
        DisplayName = sortableItem.DisplayName;
        GroupName = SortableItem.ModName;
        
        MoveUp = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(SortableItem, delta: 1);
                return Unit.Default;
            }
        );
        
        MoveDown = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(SortableItem, delta: -1);
                return Unit.Default;
            }
        );
        
        MoveTo = ReactiveCommand.CreateFromTask<int, Unit>(async index =>
            {
                await sortableItem.SortableItemProvider.SetRelativePosition(SortableItem, index - SortIndex);
                return Unit.Default;
            }
        );
    }

    public ISortableItem SortableItem { get; }
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public ReactiveCommand<int, Unit> MoveTo { get; } 
    public int SortIndex { get; }
    public string DisplayName { get; }
    public string GroupName { get; }
}
