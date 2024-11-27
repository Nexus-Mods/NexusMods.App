using NexusMods.App.UI.Controls;
using R3;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderItemModel : ITreeDataGridItemModel<ILoadOrderItemModel, Guid>
{
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }
    
    public string ModName { get; }
    public bool IsActive { get; }
}
