using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.App.UI.RightContent.LoadoutGrid;

namespace NexusMods.App.UI.Controls.DataGrid;

/// <summary>
/// The standard data grid column factory.
/// </summary>
/// <typeparam name="TVm"></typeparam>
/// <typeparam name="TRow"></typeparam>
/// <typeparam name="TColumnType"></typeparam>
public class DataGridColumnFactory<TVm, TRow, TColumnType> : IDataGridColumnFactory<TColumnType>
    where TVm : IColumnViewModel<TRow> 
    where TColumnType : struct, Enum
{
    private readonly IServiceProvider _provider;
    public TColumnType Type { get; set; }
    public DataGridLength Width { get; set; } = DataGridLength.Auto;

    public DataGridColumnFactory(IServiceProvider provider)
    {
        _provider = provider;
        
        // Chose this because trimmer friendly. 
        // This value is overwritten at runtime anyway, but we must have a sensible default.
        Type = (TColumnType)Enum.ToObject(typeof(TColumnType), 0); 
    }

    public DataGridColumn Generate()
    {
        var column = _provider.GetRequiredService<DataGridViewModelColumn<TVm, TRow>>();
        column.Width = Width;
        return column;
    }
}
