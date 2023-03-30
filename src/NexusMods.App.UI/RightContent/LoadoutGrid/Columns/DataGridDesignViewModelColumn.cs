using Avalonia.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DataGridDesignViewModelColumn<TVm, TRow> : ADataGridViewModelColumn<TVm, TRow> where TVm : class, IColumnViewModel<TRow> {
    private readonly Func<TRow,IViewFor<TVm>> _ctor;

    public DataGridDesignViewModelColumn(Func<TRow, IViewFor<TVm>> ctor)
    {
        _ctor = ctor;
    }


    protected override Control GenerateElement(DataGridCell cell, object dataItem)
    {
        try
        {
            return (Control)_ctor((TRow)dataItem);
        }
        catch (Exception ex)
        {
            return new TextBox() { Text = ex.ToString() };
        }
    }

    protected override int Compare(TRow rowType, TRow rowType1)
    {
        return 0;
    }
}
