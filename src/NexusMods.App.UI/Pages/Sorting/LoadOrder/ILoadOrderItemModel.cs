using System.Reactive;
using NexusMods.App.UI.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.Pages.Sorting;

public interface ILoadOrderItemModel : ITreeDataGridItemModel<ILoadOrderItemModel, Guid>
{
    public ReactiveCommand<Unit, Unit> MoveUp { get; }
    public ReactiveCommand<Unit, Unit> MoveDown { get; }
    public int SortIndex { get; }
    public string DisplayName { get; }
    
    public string ModName { get; }
    public bool IsActive { get; }
    public string DisplaySortIndex { get; }
    
    /// <summary>
    /// Converts a zero-based index to an ordinal number string with the appropriate suffix.
    /// </summary>
    /// <param name="sortIndex">The zero-based index to convert</param>
    /// <returns>A string representing the ordinal number with the appropriate suffix.</returns>
    internal static string ConvertZeroIndexToOrdinalNumber(int sortIndex)
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
