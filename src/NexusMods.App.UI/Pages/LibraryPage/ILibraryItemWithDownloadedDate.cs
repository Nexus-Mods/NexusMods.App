using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemWithDownloadedDate : ILibraryItemModel, IComparable<ILibraryItemWithDownloadedDate>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>
{
    ReactiveProperty<DateTimeOffset> DownloadedDate { get; }
    BindableReactiveProperty<string> FormattedDownloadedDate { get; }

    static void FormatDate(ILibraryItemWithDownloadedDate self, DateTimeOffset now) => self.FormattedDownloadedDate.Value = self.DownloadedDate.Value.FormatDate(now: now);

    int IComparable<ILibraryItemWithDownloadedDate>.CompareTo(ILibraryItemWithDownloadedDate? other) => other is null ? 1 : DateTimeOffset.Compare(DownloadedDate.Value, other.DownloadedDate.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemDownloadedDateColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>.GetColumnHeader() => "Downloaded";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithDownloadedDate>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
