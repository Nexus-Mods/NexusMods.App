using Avalonia.Media.Imaging;
using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithThumbnail : ILibraryItemModel, IComparable<ILibraryItemWithThumbnail>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnail>
{
    BindableReactiveProperty<Bitmap> Thumbnail { get; }
    
    int IComparable<ILibraryItemWithThumbnail>.CompareTo(ILibraryItemWithThumbnail? other) => 0; // not sortable

    public const string ColumnTemplateResourceKey = "LibraryItemThumbnailColumnTemplate";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnail>.GetColumnHeader() => "Thumbnail";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnail>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
