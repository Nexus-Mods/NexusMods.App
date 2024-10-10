using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithInstalledDate : ILibraryItemModel, IComparable<ILibraryItemWithInstalledDate>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>
{
    ReactiveProperty<DateTime> InstalledDate { get; }
    BindableReactiveProperty<string> FormattedInstalledDate { get; }

    int IComparable<ILibraryItemWithInstalledDate>.CompareTo(ILibraryItemWithInstalledDate? other) => other is null ? 1 : DateTime.Compare(InstalledDate.Value, other.InstalledDate.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemInstalledDateColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>.GetColumnHeader() => "Installed";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithInstalledDate>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
