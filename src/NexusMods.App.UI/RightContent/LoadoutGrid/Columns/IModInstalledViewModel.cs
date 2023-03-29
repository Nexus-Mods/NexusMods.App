using NexusMods.DataModel.Loadouts.Cursors;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IModInstalledViewModel : IColumnViewModel<ModCursor>
{
    public DateTime Installed { get; }
}
