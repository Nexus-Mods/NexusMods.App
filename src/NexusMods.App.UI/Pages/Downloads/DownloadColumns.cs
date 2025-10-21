using NexusMods.App.UI.Controls;
using NexusMods.App.UI.Extensions;

namespace NexusMods.App.UI.Pages.Downloads;

/// <summary>
/// Defines column template resource keys and component keys for Downloads.
/// </summary>
public static class DownloadColumns
{
    /// <summary>
    /// NAME+ICON COLUMN
    /// - Components: NameComponent + ImageComponent  
    /// - Shows download/mod name with icon
    /// - Sorted alphabetically by name
    /// </summary>
    public sealed class Name : ICompositeColumnDefinition<Name>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<NameComponent>(NameComponentKey);
            var bValue = b.GetOptional<NameComponent>(NameComponentKey);
            return aValue.Compare(bValue);
        }

        public const string ColumnTemplateResourceKey = nameof(DownloadColumns) + "_" + nameof(Name);
        public static readonly ComponentKey NameComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(NameComponent));
        public static readonly ComponentKey ImageComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(ImageComponent));

        public static string GetColumnHeader() => "Name";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    /// <summary>
    /// GAME COLUMN
    /// - Maps to: DownloadComponents.GameComponent (from DownloadComponents.cs)
    /// - Shows: Game name + game icon
    /// - Sortable by game name alphabetically
    /// </summary>
    public sealed class Game : ICompositeColumnDefinition<Game>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DownloadComponents.GameComponent>(ComponentKey);
            var bValue = b.GetOptional<DownloadComponents.GameComponent>(ComponentKey);
            return (aValue.HasValue, bValue.HasValue) switch
            {
                (true, true) => aValue.Value.CompareTo(bValue.Value),
                (true, false) => 1,
                (false, true) => -1,
                (false, false) => 0,
            };
        }

        public const string ColumnTemplateResourceKey = nameof(DownloadColumns) + "_" + nameof(Game);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DownloadComponents.GameComponent));

        public static string GetColumnHeader() => "Game";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    /// <summary>
    /// SIZE COLUMN
    /// - Maps to: DownloadComponents.SizeProgressComponent (from DownloadComponents.cs)
    /// - Format: "15.6MB of 56MB" (downloaded of total)
    /// - Sortable by total file size
    /// </summary>
    public sealed class Size : ICompositeColumnDefinition<Size>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DownloadComponents.SizeProgressComponent>(ComponentKey);
            var bValue = b.GetOptional<DownloadComponents.SizeProgressComponent>(ComponentKey);
            return (aValue.HasValue, bValue.HasValue) switch
            {
                (true, true) => aValue.Value.CompareTo(bValue.Value),
                (true, false) => 1,
                (false, true) => -1,
                (false, false) => 0,
            };
        }

        public const string ColumnTemplateResourceKey = nameof(DownloadColumns) + "_" + nameof(Size);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DownloadComponents.SizeProgressComponent));

        public static string GetColumnHeader() => "Size";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    /// <summary>
    /// SPEED COLUMN
    /// - Maps to: DownloadComponents.SpeedComponent (from DownloadComponents.cs)  
    /// - Shows: Transfer rate "5.2 MB/s" or "--" when inactive
    /// - Sortable by current transfer rate
    /// </summary>
    public sealed class Speed : ICompositeColumnDefinition<Speed>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DownloadComponents.SpeedComponent>(ComponentKey);
            var bValue = b.GetOptional<DownloadComponents.SpeedComponent>(ComponentKey);
            return (aValue.HasValue, bValue.HasValue) switch
            {
                (true, true) => aValue.Value.CompareTo(bValue.Value),
                (true, false) => 1,
                (false, true) => -1,
                (false, false) => 0,
            };
        }

        public const string ColumnTemplateResourceKey = nameof(DownloadColumns) + "_" + nameof(Speed);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DownloadComponents.SpeedComponent));

        public static string GetColumnHeader() => "Speed";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    /// <summary>
    /// STATUS COLUMN
    /// - Maps to: DownloadComponents.StatusComponent (from DownloadComponents.cs)
    /// - Contains: Progress bar + Pause/Resume + Cancel + Kebab menu
    /// - Consolidated all download actions into single column
    /// - Sortable by completion percentage (0% to 100%)
    /// </summary>
    public sealed class Status : ICompositeColumnDefinition<Status>
    {
        public static int Compare<TKey>(CompositeItemModel<TKey> a, CompositeItemModel<TKey> b) where TKey : notnull
        {
            var aValue = a.GetOptional<DownloadComponents.StatusComponent>(ComponentKey);
            var bValue = b.GetOptional<DownloadComponents.StatusComponent>(ComponentKey);
            return (aValue.HasValue, bValue.HasValue) switch
            {
                (true, true) => aValue.Value.Progress.Value.CompareTo(bValue.Value.Progress.Value),
                (true, false) => 1,
                (false, true) => -1,
                (false, false) => 0,
            };
        }

        public const string ColumnTemplateResourceKey = nameof(DownloadColumns) + "_" + nameof(Status);
        public static readonly ComponentKey ComponentKey = ComponentKey.From(ColumnTemplateResourceKey + "_" + nameof(DownloadComponents.StatusComponent));

        public static string GetColumnHeader() => "Status";
        public static string GetColumnTemplateResourceKey() => ColumnTemplateResourceKey;
    }

    public static readonly ComponentKey DownloadRefComponentKey = ComponentKey.From("Downloads_DownloadRef");
}
