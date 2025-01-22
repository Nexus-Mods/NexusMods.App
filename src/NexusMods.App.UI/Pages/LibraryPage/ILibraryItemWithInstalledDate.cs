using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

[Obsolete("Use CompositeItemModel instead")]
public interface ILibraryItemWithInstalledDate : ILibraryItemModel, IComparable<ILibraryItemWithInstalledDate>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>
{
    ReactiveProperty<DateTimeOffset> InstalledDate { get; }
    BindableReactiveProperty<string> FormattedInstalledDate { get; }

    static void FormatDate(ILibraryItemWithInstalledDate self, DateTimeOffset now) => self.FormattedInstalledDate.Value = self.InstalledDate.Value.FormatDate(now: now);

    int IComparable<ILibraryItemWithInstalledDate>.CompareTo(ILibraryItemWithInstalledDate? other) => other is null ? 1 : DateTimeOffset.Compare(InstalledDate.Value, other.InstalledDate.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemInstalledDateColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>.GetColumnHeader() => "Installed";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
