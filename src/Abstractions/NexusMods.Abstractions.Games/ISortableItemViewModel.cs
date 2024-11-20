using NexusMods.Abstractions.UI;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// View model interface for a sortable item in the generic load order view
/// </summary>
public interface ISortableItemViewModel : IViewModelInterface
{
    public ISortableItem SortableItem { get; }
    
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    
    public ReactiveCommand<int, Unit> MoveTo { get; }
    
    public int SortIndex { get; }
    
    public string DisplayName { get; }
    
    public string GroupName { get; }
}
