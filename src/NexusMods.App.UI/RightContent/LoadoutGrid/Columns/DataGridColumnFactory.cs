using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

public class DataGridColumnFactory<TVm, TRow> : IDataGridColumnFactory
    where TVm : IColumnViewModel<TRow>
{
    private readonly IServiceProvider _provider;

    public DataGridColumnFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public object Header { get; set; } = "";

    public DataGridColumn Generate()
    {
        var column = _provider.GetRequiredService<DataGridViewModelColumn<TVm, TRow>>();
        column.Header = Header;
        return column;
    }
}
