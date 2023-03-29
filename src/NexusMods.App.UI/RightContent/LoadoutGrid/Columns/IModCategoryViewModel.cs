using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModCategoryViewModel : IColumnViewModel<ModCursor>
{
    public string Category { get; }
}
