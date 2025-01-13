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
            var aValue = a.GetOptional<SharedComponents.Name>();
            var bValue = b.GetOptional<SharedComponents.Name>();

            return aValue.Compare(bValue, static (a, b) => string.CompareOrdinal(a.Value.Value, b.Value.Value));
        }

        public const string ColumnTemplateResourceKey = Prefix + "Name";
        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public sealed class InstalledDate : ICompositeColumnDefinition<InstalledDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SharedComponents.InstalledDate>();
            var bValue = b.GetOptional<SharedComponents.InstalledDate>();

            return aValue.Compare(bValue, static (a, b) => a.Value.Value.CompareTo(b.Value.Value));
        }

        public const string ColumnTemplateResourceKey = Prefix + "InstalledDate";
        public static string GetColumnHeader() => "Installed";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public sealed class DownloadedDate : ICompositeColumnDefinition<DownloadedDate>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<SharedComponents.DownloadedDate>();
            var bValue = b.GetOptional<SharedComponents.DownloadedDate>();

            return aValue.Compare(bValue, static (a, b) => a.Value.Value.CompareTo(b.Value.Value));
        }

        public const string ColumnTemplateResourceKey = Prefix + "DownloadedDate";
        public static string GetColumnHeader() => "Downloaded";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
}
