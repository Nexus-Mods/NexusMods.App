using Avalonia.Controls.Models.TreeDataGrid;

namespace NexusMods.App.UI.Controls;

public class CustomTextCell : ICell
{
    public ICell Inner { get; }
    public string Id { get; }

    public CustomTextCell(ICell inner, string id)
    {
        Inner = inner;
        Id = id;
    }

    public bool CanEdit => Inner.CanEdit;
    public BeginEditGestures EditGestures => Inner.EditGestures;
    public object? Value => Inner.Value;
}
