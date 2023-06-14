using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;

public class ModCategoryDesignViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel
{
    public ModCursor Row { get; set; }
    public string Category { get; } = "Some Category";

    public static ColumnType Type => ColumnType.Category;
}
