﻿using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;

namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// Abstract base
/// </summary>
/// <typeparam name="TVmType"></typeparam>
/// <typeparam name="TRowType"></typeparam>
// ReSharper disable once UnusedTypeParameter
public abstract class ADataGridViewModelColumn<TVmType, TRowType> : DataGridTemplateColumn where TVmType : IColumnViewModel<TRowType>
{
    /// <summary>
    /// Default constructor, sets the column to sortable, reorderable, and resizable and read-only.
    /// </summary>
    public ADataGridViewModelColumn()
    {
        IsReadOnly = true;
        CanUserSort = true;
        CanUserResize = true;
    }

    protected override void RefreshCellContent(Control element, string propertyName)
    {
        if (propertyName == nameof(CellTemplate) && element.Parent is DataGridCell cell)
        {
            cell.Content = GenerateElement(cell, cell.DataContext);
        }
    }

    protected override Control GenerateEditingElement(DataGridCell cell, object dataItem,
        out ICellEditBinding binding)
    {
        throw new NotImplementedException();
    }
    protected override object PrepareCellForEdit(Control editingElement,
        RoutedEventArgs editingEventArgs)
    {
        throw new NotImplementedException();
    }
}
