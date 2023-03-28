using NexusMods.DataModel.Loadouts;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModNameViewModel : IColumnViewModel<ModId>
{
    public string Name { get; }
}
