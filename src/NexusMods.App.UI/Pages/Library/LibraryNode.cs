using System.ComponentModel;
using Avalonia.Controls.Models.TreeDataGrid;
using NexusMods.App.UI.Controls;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.Library;

public readonly struct LibraryNodeId
{
    public readonly ulong Prefix;
    public readonly EntityId Id;

    public LibraryNodeId(ulong prefix, EntityId id)
    {
        Prefix = prefix;
        Id = id;
    }

    public static LibraryNodeId Empty => new();

    public static implicit operator LibraryNodeId(EntityId id) => new(0, id);
    public static implicit operator EntityId(LibraryNodeId id) => id.Id;
}

public class LibraryNode : Node<LibraryNode>
{
    public required LibraryNodeId Id { get; init; }
    [Reactive] public LibraryNodeId ParentId { get; set; }

    public required string Name { get; init; }

    protected const string DefaultVersion = "-";
    [Reactive] public string Version { get; set; } = DefaultVersion;

    protected static readonly Size DefaultSize = Size.Zero;
    [Reactive] public Size Size { get; set; } = DefaultSize;

    public static IColumn<LibraryNode> CreateNameColumn()
    {
        return new TextColumn<LibraryNode, string>(
            header: "Name",
            getter: model => model.Name,
            options: new TextColumnOptions<LibraryNode>
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
            Tag = "name",
        };
    }

    public static IColumn<LibraryNode> CreateVersionColumn()
    {
        return new TextColumn<LibraryNode, string>(
            header: "Version",
            getter: model => model.Version,
            options: new TextColumnOptions<LibraryNode>
            {
                CompareAscending = static (a, b) => string.Compare(a?.Version, b?.Version, StringComparison.OrdinalIgnoreCase),
                CompareDescending = static (a, b) => string.Compare(b?.Version, a?.Version, StringComparison.OrdinalIgnoreCase),
                IsTextSearchEnabled = true,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Tag = "version",
        };
    }

    public static IColumn<LibraryNode> CreateSizeColumn()
    {
        return new TextColumn<LibraryNode, Size>(
            header: "Size",
            getter: model => model.Size,
            options: new TextColumnOptions<LibraryNode>
            {
                CompareAscending = static (a, b) => a is null ? -1 : a.Size.CompareTo(b?.Size ?? Size.Zero),
                CompareDescending = static (a, b) => b is null ? -1 : b.Size.CompareTo(a?.Size ?? Size.Zero),
                IsTextSearchEnabled = false,
                CanUserResizeColumn = true,
                CanUserSortColumn = true,
            }
        )
        {
            Tag = "size",
        };
    }
}
