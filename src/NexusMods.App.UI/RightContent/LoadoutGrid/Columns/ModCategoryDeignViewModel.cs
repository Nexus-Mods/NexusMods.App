using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class ModCategoryDeignViewModel : AViewModel<IModCategoryViewModel>, IModCategoryViewModel
{
    public ModCursor Row { get; set; }
    public string Category { get; } = "Some Category";
}
