using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithDownloadedDate : ILibraryItemModel, IComparable<ILibraryItemWithDownloadedDate>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>
{
    ReactiveProperty<DateTime> DownloadedDate { get; }
    BindableReactiveProperty<string> FormattedDownloadedDate { get; }

    int IComparable<ILibraryItemWithDownloadedDate>.CompareTo(ILibraryItemWithDownloadedDate? other) => other is null ? 1 : DateTime.Compare(DownloadedDate.Value, other.DownloadedDate.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemDownloadedDateColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>.GetColumnHeader() => "Downloaded";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
