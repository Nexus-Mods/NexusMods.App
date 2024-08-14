using System.Linq.Expressions;
using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;

namespace NexusMods.App.UI.Controls;

public class CustomTextColumn<TModel, TValue> : TextColumn<TModel, TValue>
    where TModel : class
{
    public required string Id { get; init; }

    public CustomTextColumn(object? header, Expression<Func<TModel, TValue?>> getter, GridLength? width = null, TextColumnOptions<TModel>? options = null)
        : base(header, getter, width, options) { }

    public CustomTextColumn(object? header, Expression<Func<TModel, TValue?>> getter, Action<TModel, TValue?> setter, GridLength? width = null, TextColumnOptions<TModel>? options = null)
        : base(header, getter, setter, width, options) { }

    public override ICell CreateCell(IRow<TModel> row)
    {
        var inner = base.CreateCell(row);
        return new CustomTextCell(inner: inner, id: Id);
    }
}
