using JetBrains.Annotations;
using NexusMods.App.UI.Extensions;
using NexusMods.App.UI.Pages.ItemContentsFileTree.New;

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
            return aValue.Compare(bValue);
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
    
    /// <summary>
    /// An abstract base class for uint count columns.
    /// </summary>
    /// <remarks>
    ///     This is meant to be inherited to allow for renaming the column header.
    ///     For an example, see <see cref="Columns.FileCount"/>
    /// </remarks>
    public abstract class UIntCount<TSelf> : ICompositeColumnDefinition<UIntCount<TSelf>>
        where TSelf : IHaveColumnHeader
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<UIntComponent>(UIntComponentKey);
            var bValue = b.GetOptional<UIntComponent>(UIntComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "UIntCount";
        public static readonly ComponentKey UIntComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(UIntComponent));

        // Default implementation that can be overridden
        public static string GetColumnHeader() => TSelf.ColumnHeader;
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    public interface IHaveColumnHeader
    {
        static abstract string ColumnHeader { get; }
    }
}
