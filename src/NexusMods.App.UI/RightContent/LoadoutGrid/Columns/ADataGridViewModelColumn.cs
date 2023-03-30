using System.Collections;
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
        CanUserSort = true;
        CanUserReorder = true;
        CanUserResize = true;
        CustomSortComparer = new Comparer(this);
    }

    private class Comparer : IComparer
    {
        private readonly ADataGridViewModelColumn<TVmType,TRowType> _column;
        public Comparer(ADataGridViewModelColumn<TVmType, TRowType> column)
        {
            _column = column;
        }

        public int Compare(object? x, object? y)
        {
            return _column.Compare((TRowType)x!, (TRowType)y!);
        }
    }

    protected abstract int Compare(TRowType rowType, TRowType rowType1);

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
