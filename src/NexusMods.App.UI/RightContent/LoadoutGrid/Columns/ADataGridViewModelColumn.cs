using System.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public abstract class ADataGridViewModelColumn<TVmType, TRowType> : DataGridTemplateColumn where TVmType : IColumnViewModel<TRowType>
{
    public ADataGridViewModelColumn()
    {
        IsReadOnly = true;
        CanUserSort = true;
        CanUserReorder = true;
        CanUserResize = true;


    }

    protected override void RefreshCellContent(Control element, string propertyName)
    {
        var cell = element.Parent as DataGridCell;
        if (propertyName == nameof(CellTemplate) && cell is not null)
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
