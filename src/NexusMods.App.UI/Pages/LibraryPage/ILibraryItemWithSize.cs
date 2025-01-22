using NexusMods.App.UI.Controls;
using NexusMods.Paths;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemWithSize : ILibraryItemModel, IComparable<ILibraryItemWithSize>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithSize>
{
    ReactiveProperty<Size> ItemSize { get; }
    BindableReactiveProperty<string> FormattedSize { get; }

    int IComparable<ILibraryItemWithSize>.CompareTo(ILibraryItemWithSize? other) => other is null ? 1 : ItemSize.Value.CompareTo(other.ItemSize.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemSizeColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithSize>.GetColumnHeader() => "Size";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithSize>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
