using Avalonia.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// Datagrid column factory that creates a column that uses a design-time view model.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
public class DataGridColumnDesignFactory<TVm, TRow> : IDataGridColumnFactory where TVm : class, IColumnViewModel<TRow>
{
    private readonly Func<TRow,IViewFor<TVm>> _ctor;
    public ColumnType Type { get; }

    public DataGridColumnDesignFactory(Func<TRow, IViewFor<TVm>> ctor, ColumnType type)
    {
        Type = type;
        _ctor = ctor;
    }

    public DataGridColumn Generate()
    {
        return new DataGridDesignViewModelColumn<TVm, TRow>(_ctor);
    }
}
