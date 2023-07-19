using Avalonia.Controls;

namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// DataGridColumns can only be assigned to a DataGrid once, so when we have
/// reactive bindings sometimes we need to update columns, or re-assign them.
/// So instead we use this factory to generate new columns.
/// </summary>
public interface IDataGridColumnFactory<TColumnType> where TColumnType : Enum
{
    /// <summary>
    /// Creates a new instance of a DataGridColumn.
    /// </summary>
    /// <returns></returns>
    DataGridColumn Generate();

    /// <summary>
    /// The type of the column.
    /// </summary>
    TColumnType Type { get; }
}
