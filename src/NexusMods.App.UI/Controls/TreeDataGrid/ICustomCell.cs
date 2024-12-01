using Avalonia.Controls.Models.TreeDataGrid;

namespace NexusMods.App.UI.Controls;

public interface ICustomCell : ICell
{
    public string Id { get; }
    public bool IsRoot { get; }
}
