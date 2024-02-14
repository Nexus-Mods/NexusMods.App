using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.App.UI.Pages.LoadoutGrid.Columns.ModCategory;

public class ModCategoryDesignViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel
{
    public ModCursor Row { get; set; }
    public string Category { get; } = "Some Category";

    public static LoadoutColumn Type => LoadoutColumn.Category;
}
