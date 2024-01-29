using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Loadouts.Mods;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.ModCategory;

public class ModCategoryDesignViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel
{
    public ModCursor Row { get; set; }
    public string Category { get; } = "Some Category";

    public static LoadoutColumn Type => LoadoutColumn.Category;
}
