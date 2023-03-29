using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModVersionViewModel : IColumnViewModel<ModCursor>
{
    public string Version { get; }
}
