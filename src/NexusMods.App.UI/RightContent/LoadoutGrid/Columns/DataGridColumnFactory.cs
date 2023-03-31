using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// The standard data grid column factory.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
public class DataGridColumnFactory<TVm, TRow> : IDataGridColumnFactory
    where TVm : IColumnViewModel<TRow>
{
    private readonly IServiceProvider _provider;
    public ColumnType Type { get; set; } = ColumnType.Name;

    public DataGridColumnFactory(IServiceProvider provider)
    {
        _provider = provider;
    }

    public DataGridColumn Generate()
    {
        return _provider.GetRequiredService<DataGridViewModelColumn<TVm, TRow>>();
    }
}
