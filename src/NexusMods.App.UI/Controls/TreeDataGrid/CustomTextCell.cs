using Avalonia.Controls.Models.TreeDataGrid;

namespace NexusMods.App.UI.Controls;

public class CustomTextCell : ICell
{
    public ICell Inner { get; }
    public string Id { get; }
    public bool IsRoot { get; }

    public CustomTextCell(ICell inner, string id, bool isRoot)
    {
        Inner = inner;
        Id = id;
        IsRoot = isRoot;
    }

    public bool CanEdit => Inner.CanEdit;
    public BeginEditGestures EditGestures => Inner.EditGestures;
    public object? Value => Inner.Value;
}
