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
    public string SortOrdinalNumber => ConvertZeroIndexToOrdinalNumber(SortIndex);
    
    private string ConvertZeroIndexToOrdinalNumber(int sortIndex)
    {
        var displayIndex = sortIndex + 1;
        var suffix = displayIndex switch
        {
            11 or 12 or 13 => "th",
            _ when displayIndex % 10 == 1 => "st",
            _ when displayIndex % 10 == 2 => "nd",
            _ when displayIndex % 10 == 3 => "rd",
            _ => "th"
        };
        return $"{displayIndex}{suffix}";
    }
}
