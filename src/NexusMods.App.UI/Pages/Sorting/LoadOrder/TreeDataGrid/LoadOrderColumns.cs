using JetBrains.Annotations;
using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.Pages.Sorting;


public static class LoadOrderColumns
{
    [UsedImplicitly]
    public sealed class IndexColumn : ICompositeColumnDefinition<IndexColumn>
    {
        public const string ColumnTemplateResourceKey = nameof(LoadOrderColumns) + "_" + nameof(IndexColumn);

        public static readonly ComponentKey IndexComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(SharedComponents.IndexComponent));

        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        // The header name should be set on column creation as it is game dependent
        public static string GetColumnHeader() => throw new NotSupportedException();

        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;

    }

    [UsedImplicitly]
    public sealed class DisplayNameColumn : ICompositeColumnDefinition<DisplayNameColumn>
    {
        public const string ColumnTemplateResourceKey = nameof(LoadOrderColumns) + "_" + nameof(DisplayNameColumn);

        public static readonly ComponentKey DisplayNameComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "DisplayNameComponent");
        public static readonly ComponentKey ImageComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(ImageComponent));
        
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        // The header name should be set on column creation as it is game dependent
        public static string GetColumnHeader() => throw new NotSupportedException();

        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }
    
    [UsedImplicitly]
    public sealed class ModNameColumn : ICompositeColumnDefinition<ModNameColumn>
    {
        public const string ColumnTemplateResourceKey = nameof(LoadOrderColumns) + "_" + nameof(ModNameColumn);

        public static readonly ComponentKey ModNameComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + "ModNameComponent");
        
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            throw new NotImplementedException();
        }

        // The header name should be set on column creation as it is game dependent
        public static string GetColumnHeader() => "Parent mod";

        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public static readonly ComponentKey IsActiveComponentKey = ComponentKey.From(nameof(LoadOrderColumns) + "_" + "IsActiveComponent");
    public static readonly string IsActiveStyleTag = "IsActive";
}
