using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithName : ILibraryItemModel, IComparable<ILibraryItemWithName>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithName>
{
    BindableReactiveProperty<string> Name { get; }

    int IComparable<ILibraryItemWithName>.CompareTo(ILibraryItemWithName? other) => string.CompareOrdinal(Name.Value, other?.Name.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemNameColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithName>.GetColumnHeader() => "Name";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithName>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
