using System.Reactive;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.UI;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.Games.RedEngine.Cyberpunk2077;

public class RedModObservableSortableItemViewModel : AViewModel<IObservableSortableItemViewModel>, IObservableSortableItemViewModel
{
    private RedModSortableItem _sortableItem;
    public RedModObservableSortableItemViewModel(RedModSortableItem sortableItem)
    {
        _sortableItem = sortableItem;
        // TODO: Implement
        GroupName = "";
        IsEnabled = true;
        InCollections = [];

        MoveUp = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SetRelativePosition(delta: 1);
                return Unit.Default;
            }
        );

        MoveDown = ReactiveCommand.CreateFromTask(async () =>
            {
                await sortableItem.SetRelativePosition(delta: -1);
                return Unit.Default;
            }
        );

        MoveTo = ReactiveCommand.CreateFromTask<int, Unit>(async index =>
            {
                await sortableItem.SetRelativePosition(index - SortIndex);
                return Unit.Default;
            }
        );

        SetEnabled = ReactiveCommand.Create<bool, Unit>(isEnabled =>
            {
                // TODO: Implment
                IsEnabled = isEnabled;
                return Unit.Default;
            }
        );
    }

    public string Name => _sortableItem.Name;
    public int SortIndex => _sortableItem.SortIndex;
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public ReactiveCommand<int, Unit> MoveTo { get; }
    public string GroupName { get; }
    public bool IsEnabled { get; set; }
    public ReactiveCommand<bool, Unit> SetEnabled { get; }

    public string[] InCollections { get; }
}
