using Avalonia.Controls;
using Mutagen.Bethesda.Skyrim;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// DataGridColumns can only be assigned to a DataGrid once, so when we have
/// reactive bindings sometimes we need to update columns, or re-assign them.
/// So instead we use this factory to generate new columns.
/// </summary>
public interface IDataGridColumnFactory
{
    /// <summary>
    /// Creates a new instance of a DataGridColumn.
    /// </summary>
    /// <returns></returns>
    DataGridColumn Generate();

    /// <summary>
    /// The type of the column.
    /// </summary>
    ColumnType Type { get; }
}
