using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;

namespace NexusMods.App.UI.Controls;

[PublicAPI]
public static class SharedColumns
{
    private const string Prefix = "SharedColumn_";

    public sealed class Name : ICompositeColumnDefinition<Name>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<StringComponent>(StringComponentKey);
            var bValue = b.GetOptional<StringComponent>(StringComponentKey);
            return aValue.Compare(bValue, (x, y) => StringComparer.OrdinalIgnoreCase.Compare(x.Value.Value, y.Value.Value));
        }

        public const string ColumnTemplateResourceKey = Prefix + "Name";
        public static readonly ComponentKey StringComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(StringComponent));
        public static readonly ComponentKey ImageComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(ImageComponent));

        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public sealed class InstalledDate : ICompositeColumnDefinition<InstalledDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DateComponent>(ComponentKey);
            var bValue = b.GetOptional<DateComponent>(ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "InstalledDate";
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DateComponent));

        public static string GetColumnHeader() => "Installed";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    [UsedImplicitly]
    public sealed class ItemSize : ICompositeColumnDefinition<ItemSize>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SizeComponent>(key: ComponentKey);
            var bValue = b.GetOptional<SizeComponent>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "_" + nameof(ItemSize);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SizeComponent));
        public static string GetColumnHeader() => "Size";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    /// <summary>
    /// Represents a file or folder name, accompanied by a file or folder icon.
    /// </summary>
    [UsedImplicitly]
    public sealed class NameWithFileIcon : ICompositeColumnDefinition<NameWithFileIcon>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<StringComponent>(StringComponentKey);
            var bValue = b.GetOptional<StringComponent>(StringComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "NameWithFileIcon";
        public static readonly ComponentKey StringComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(StringComponent));
        public static readonly ComponentKey IconComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(UnifiedIconComponent));

        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    /// <summary>
    /// Represents a count of files that lives under a folder. 
    /// </summary>
    [UsedImplicitly]
    public sealed class FileCount : ICompositeColumnDefinition<FileCount>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<UInt32Component>(ComponentKey);
            var bValue = b.GetOptional<UInt32Component>(ComponentKey);
            // Assuming ValueComponent has a comparable Value property or implements IComparable
            // Adjust the comparison logic if ValueComponent comparison needs specific handling
            return aValue.Compare(bValue); 
        }

        public const string ColumnTemplateResourceKey = Prefix + nameof(FileCount);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "FileCount");

        public static string GetColumnHeader() => "File Count";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    /// <summary>
    /// A variant of <see cref="ItemSize"/> used with <see cref="CompositeItemModel{GamePath}"/> as opposed to
    /// <see cref="CompositeItemModel{EntityId}"/>
    /// </summary>
    [UsedImplicitly]
    public sealed class ItemSizeOverGamePath : ICompositeColumnDefinition<ItemSizeOverGamePath>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SizeComponent>(key: ComponentKey);
            var bValue = b.GetOptional<SizeComponent>(key: ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "_GamePath_" + nameof(ItemSize);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_GamePath_" + nameof(SizeComponent));
        public static string GetColumnHeader() => "Size";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
