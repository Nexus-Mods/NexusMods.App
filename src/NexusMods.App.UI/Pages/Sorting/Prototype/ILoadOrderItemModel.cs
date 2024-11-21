using System.Reactive;
using NexusMods.Abstractions.Games;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting.Prototype;

public interface ILoadOrderItemModel : ITreeDataGridItemModel<ILoadOrderItemModel, Guid>
{
    public ISortableItem InnerItem { get; }
    public Guid Id { get; }
    
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }
}
