using NexusMods.Abstractions.UI;
using ReactiveUI;
using Unit = System.Reactive.Unit;

namespace NexusMods.Abstractions.Games.UI;

public interface IObservableSortableItemViewModel : IViewModelInterface
{
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    
    public ReactiveCommand<int, Unit> MoveTo { get; }
    
    public int SortIndex { get; }
    
    public string GroupName { get; }
    
    public string Name { get; }
    
    public bool IsEnabled { get; }
    
    public ReactiveCommand<bool, Unit> SetEnabled { get; }
    
    public string[] InCollections { get; }
}
