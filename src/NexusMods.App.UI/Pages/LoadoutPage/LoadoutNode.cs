using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.Pages.LoadoutPage;

public class LoadoutNode : Node<LoadoutNode>
{
    public required string Name { get; init; }

    public static IColumn<LoadoutNode> CreateNameColumn()
    {
        return new CustomTextColumn<LoadoutNode, string>(
            header: "Name",
            getter: model => model.Name,
            options: new TextColumnOptions<LoadoutNode>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Name, a?.Name, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            SortDirection = ListSortDirection.Ascending,
            Id = "name",
        };
    }
}
