using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace NexusMods.App.UI.Controls;

public class CustomTemplateColumn<TModel> : TemplateColumn<TModel>
    where TModel : class
{
    public required string Id { get; init; }

    public CustomTemplateColumn(object? header, IDataTemplate cellTemplate, IDataTemplate? cellEditingTemplate = null, GridLength? width = null, TemplateColumnOptions<TModel>? options = null) : base(header, cellTemplate, cellEditingTemplate, width, options) { }

    public CustomTemplateColumn(object? header, object cellTemplateResourceKey, object? cellEditingTemplateResourceKey = null, GridLength? width = null, TemplateColumnOptions<TModel>? options = null) : base(header, cellTemplateResourceKey, cellEditingTemplateResourceKey, width, options) { }

    public override ICell CreateCell(IRow<TModel> row)
    {
        var isRoot = true;

        if (row is HierarchicalRow<TModel> hierarchicalRow)
        {
            var indent = hierarchicalRow.Indent;
            isRoot = indent == 0;
        }

        var inner = base.CreateCell(row);
        return new CustomCell(inner: inner, id: Id, isRoot: isRoot);
    }
}
