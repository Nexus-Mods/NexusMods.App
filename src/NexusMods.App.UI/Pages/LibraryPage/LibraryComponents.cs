using NexusMods.Abstractions.UI;
using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;

namespace NexusMods.App.UI.Pages.LibraryPage;

public static class LibraryComponents
{

}

public static class LibraryColumns
{
    // public sealed class ItemVersion : ICompositeColumnDefinition<ItemVersion>
    // {
    //     public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
    //     {
    //         // var aValue = a.GetOptional<SizeComponent>(key: ComponentKey);
    //         // var bValue = b.GetOptional<SizeComponent>(key: ComponentKey);
    //         // return aValue.Compare(bValue);
    //     }
    //
    //     public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(ItemSize);
    //     // public static readonly ComponentKey ComponentKey = typeof(SizeComponent);
    //     public static string GetColumnHeader() => "Size";
    //     public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    // }

    public sealed class DownloadedDate : ICompositeColumnDefinition<DownloadedDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DateComponent>(ComponentKey);
            var bValue = b.GetOptional<DateComponent>(ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(DownloadedDate);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DateComponent));

        public static string GetColumnHeader() => "Downloaded";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public sealed class ItemSize : ICompositeColumnDefinition<ItemSize>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SizeComponent>(key: ComponentKey);
            var bValue = b.GetOptional<SizeComponent>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(LibraryColumns) + "_" + nameof(ItemSize);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SizeComponent));
        public static string GetColumnHeader() => "Size";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
