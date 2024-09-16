using Avalonia.Controls;
using Avalonia.Controls.Models.TreeDataGrid;
using Avalonia.Controls.Templates;

namespace NexusMods.App.UI.Controls;

public class CustomTemplateCell : TemplateCell, ICustomCell
{
    public required string Id { get; init; }
    public required bool IsRoot { get; init; }

    public CustomTemplateCell(object? value, Func<Control, IDataTemplate> getCellTemplate, Func<Control, IDataTemplate>? getCellEditingTemplate, ITemplateCellOptions? options) : base(value, getCellTemplate, getCellEditingTemplate, options) { }
}
