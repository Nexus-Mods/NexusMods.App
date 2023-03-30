using Avalonia.Controls;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public interface IDataGridColumnFactory
{
    /// <summary>
    /// Creates a new instance of a DataGridColumn.
    /// </summary>
    /// <returns></returns>
    DataGridColumn Generate();
}
