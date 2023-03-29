using Avalonia.Controls;
using Avalonia.Controls.Utils;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public abstract class ADataGridViewModelColumn<TVmType, TRowType> : DataGridColumn where TVmType : IColumnViewModel<TRowType>
{

    public ADataGridViewModelColumn()
    {
        IsReadOnly = true;
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
