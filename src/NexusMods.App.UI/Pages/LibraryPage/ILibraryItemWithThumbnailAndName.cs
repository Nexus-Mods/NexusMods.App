using Avalonia.Media.Imaging;
using NexusMods.App.UI.Controls;
using R3;

namespace NexusMods.App.UI.Pages.LibraryPage;

public interface ILibraryItemWithThumbnailAndName : ILibraryItemModel, IComparable<ILibraryItemWithThumbnailAndName>, IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnailAndName>
{
    BindableReactiveProperty<Bitmap> Thumbnail { get; }
    BindableReactiveProperty<string> Name { get; }
    BindableReactiveProperty<bool> ShowThumbnail { get; }

    int IComparable<ILibraryItemWithThumbnailAndName>.CompareTo(ILibraryItemWithThumbnailAndName? other) => string.CompareOrdinal(Name.Value, other?.Name.Value);

    public const string ColumnTemplateResourceKey = "LibraryItemNameColumn";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnailAndName>.GetColumnHeader() => "Name";
    static string IColumnDefinition<ILibraryItemModel, ILibraryItemWithThumbnailAndName>.GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
}
