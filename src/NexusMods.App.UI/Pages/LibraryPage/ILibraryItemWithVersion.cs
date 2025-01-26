using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemWithVersion : ILibraryItemModel, IComparable<ILibraryItemWithVersion>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithVersion>
{
    BindableReactiveProperty<string> Version { get; }

    int IComparable<ILibraryItemWithVersion>.CompareTo(ILibraryItemWithVersion? other) => string.CompareOrdinal(Version.Value, other?.Version.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemVersionColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithVersion>.GetColumnHeader() => "Version";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithVersion>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
