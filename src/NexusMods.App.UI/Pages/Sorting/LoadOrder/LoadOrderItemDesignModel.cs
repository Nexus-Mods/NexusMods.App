using System.Reactive;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public class LoadOrderItemDesignModel : TreeDataGridItemModel<ILoadOrderItemModel, Guid>, ILoadOrderItemModel
{
    public ReactiveCommand<Unit, Unit> MoveUp { get; } = ReactiveCommand.Create(() => { });
    public ReactiveCommand<Unit, Unit> MoveDown { get; } = ReactiveCommand.Create(() => { });
    public int SortIndex { get; set; }
    public string DisplayName { get; set; } = "Display Name";
    public string ModName { get; set; } = "Mod Name";
    public bool IsActive { get; set; }
    public Guid Guid { get; set; }
    public string DisplaySortIndex => ILoadOrderItemModel.ConvertZeroIndexToOrdinalNumber(SortIndex);
}
