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
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "Name";
        public static readonly ComponentKey StringComponentKey = ComponentKey.From(Prefix + nameof(Name) + nameof(StringComponent));

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
        public static readonly ComponentKey ComponentKey = ComponentKey.From(Prefix + nameof(InstalledDate) + nameof(DateComponent));

        public static string GetColumnHeader() => "Installed";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public sealed class DownloadedDate : ICompositeColumnDefinition<DownloadedDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DateComponent>(ComponentKey);
            var bValue = b.GetOptional<DateComponent>(ComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = Prefix + "DownloadedDate";
        public static readonly ComponentKey ComponentKey = ComponentKey.From(Prefix + nameof(DownloadedDate) + nameof(DateComponent));

        public static string GetColumnHeader() => "Downloaded";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
