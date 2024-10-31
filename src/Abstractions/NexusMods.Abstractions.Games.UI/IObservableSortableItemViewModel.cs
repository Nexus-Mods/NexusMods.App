using System.Reactive;
using NexusMods.Abstractions.UI;
using ReactiveUI;

namespace NexusMods.Abstractions.Games.UI;

public interface IObservableSortableItemViewModel : IViewModelInterface
{
    public IReactiveCommand<Unit, Unit> MoveUp { get; }
    
    public IReactiveCommand<Unit, Unit> MoveDown { get; }
    
    public IReactiveCommand<int, Unit> MoveTo { get; }
    
    public int SortIndex { get; }
    
    public string GroupName { get; }
    
    public string Name { get; }
    
    public bool IsEnabled { get; }
    
    public IReactiveCommand<bool, Unit> SetEnabled { get; }
    
    public string[] InCollections { get; }
}
