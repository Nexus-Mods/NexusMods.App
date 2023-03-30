using Avalonia.Controls;
using ReactiveUI;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DataGridColumnDesignFactory<TVm, TRow> : IDataGridColumnFactory where TVm : class, IColumnViewModel<TRow>
{
    private readonly Func<TRow,IViewFor<TVm>> _ctor;

    public DataGridColumnDesignFactory(Func<TRow, IViewFor<TVm>> ctor)
    {
        _ctor = ctor;
    }

    public object Header { get; init; } = "";

    public DataGridColumn Generate()
    {
        return new DataGridDesignViewModelColumn<TVm, TRow>(_ctor) {Header = Header};
    }
}
