using Avalonia.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// DataGridColumn for use in design-time.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
public class DataGridDesignViewModelColumn<TVm, TRow> : ADataGridViewModelColumn<TVm, TRow> where TVm : class, IColumnViewModel<TRow> {
    private readonly Func<TRow,IViewFor<TVm>> _ctor;

    public DataGridDesignViewModelColumn(Func<TRow, IViewFor<TVm>> ctor)
    {
        _ctor = ctor;
        CustomSortComparer = Comparer<object?>.Create((x, y) => 0);
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
}
