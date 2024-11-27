using ExCSS;
using NexusMods.App.UI.Controls;
using R3;
using Unit = System.Reactive.Unit;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderItemDesignModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    public ReactiveCommand<Unit, Unit> MoveUp { get; } = new(_ => Unit.Default);
    public ReactiveCommand<Unit, Unit> MoveDown { get; } = new(_ => Unit.Default);
    public int SortIndex { get; set; }
    public string DisplayName { get; set; } = "Display Name";
    public string ModName { get; set; } = "Mod Name";
    public bool IsActive { get; set; }
    public Guid Guid { get; set; }
}
