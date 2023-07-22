using Avalonia.Controls;
using NexusMods.App.UI.RightContent.LoadoutGrid;
using ReactiveUI;

namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// Datagrid column factory that creates a column that uses a design-time view model.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
/// <typeparam name="TColumnType"></typeparam>
public class DataGridColumnDesignFactory<TVm, TRow, TColumnType> : IDataGridColumnFactory<TColumnType> 
    where TVm : class, IColumnViewModel<TRow> 
    where TColumnType : Enum
{
    private readonly Func<TRow,IViewFor<TVm>> _ctor;
    public TColumnType Type { get; }
    
    public DataGridLength Width { get; set; } = DataGridLength.Auto;

    public DataGridColumnDesignFactory(Func<TRow, IViewFor<TVm>> ctor, TColumnType type)
    {
        Type = type;
        _ctor = ctor;
    }

    public DataGridColumn Generate()
    {
        return new DataGridDesignViewModelColumn<TVm, TRow>(_ctor) { Width = Width };
    }
}
