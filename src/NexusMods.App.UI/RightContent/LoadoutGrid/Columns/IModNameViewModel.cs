using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModNameViewModel : IColumnViewModel<ModCursor>
{
    public string Name { get; }
}
