using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New.ViewLoadoutGroupFiles;

/// <summary>
/// Columns unique to the 'View Files' view. 
/// </summary>
public static class Columns
{
    public const string SharedPrefix = "ViewFilesColumn_";
    
    /// <summary>
    /// Represents a file or folder name, accompanied by a file or folder icon.
    /// </summary>
    public sealed class NameWithFileIcon : ICompositeColumnDefinition<NameWithFileIcon>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<StringComponent>(StringComponentKey);
            var bValue = b.GetOptional<StringComponent>(StringComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = SharedPrefix + "Name";
        public static readonly ComponentKey StringComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(StringComponent));
        public static readonly ComponentKey IconComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(UnifiedIconComponent));

        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    /// <summary>
    /// Represents a count of files that lives under a folder. 
    /// </summary>
    public sealed class FileCount : ICompositeColumnDefinition<FileCount>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<ValueComponent<uint>>(ComponentKey);
            var bValue = b.GetOptional<ValueComponent<uint>>(ComponentKey);
            // Assuming ValueComponent has a comparable Value property or implements IComparable
            // Adjust the comparison logic if ValueComponent comparison needs specific handling
            return aValue.Compare(bValue); 
        }

        public const string ColumnTemplateResourceKey = SharedPrefix + nameof(FileCount);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "FileCount");

        public static string GetColumnHeader() => "File Count";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
