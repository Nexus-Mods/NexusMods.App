using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;
namespace NexusMods.App.UI.Pages.ItemContentsFileTree.New;

/// <summary>
/// Columns unique to the 'View Files' view. 
/// </summary>
public static class Columns
{
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

        public const string ColumnTemplateResourceKey = "ViewFilesColumn_" + "Name";
        public static readonly ComponentKey StringComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(StringComponent));
        public static readonly ComponentKey IconComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(UnifiedIconComponent));

        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
