using NexusMods.App.UI.Controls;

namespace NexusMods.App.UI.Pages.Downloads;

/*
 * DOWNLOADS COLUMN DEFINITIONS OVERVIEW  
 * ====================================
 * 
 * This file defines the column structure for the Downloads TreeDataGrid.
 * Each column implements ICompositeColumnDefinition<T> and maps to specific components.
 * 
 * COLUMN STRUCTURE (5 columns total):
 * 
 * 1. NAME+ICON COLUMN:
 *    - Uses SharedColumns.Name from Controls/TreeDataGrid/SharedColumns.cs
 *    - Components: NameComponent + ImageComponent  
 *    - Shows download/mod name with icon
 * 
 * 2. GAME COLUMN (defined here as DownloadColumns.Game):
 *    - Maps to: DownloadComponents.GameComponent (from DownloadComponents.cs)
 *    - Shows: Game name + game icon
 *    - Sortable by game name alphabetically
 * 
 * 3. SIZE COLUMN (defined here as DownloadColumns.Size):  
 *    - Maps to: DownloadComponents.SizeProgressComponent (from DownloadComponents.cs)
 *    - Format: "15.6MB of 56MB" (downloaded of total)
 *    - Sortable by total file size
 * 
 * 4. SPEED COLUMN (defined here as DownloadColumns.Speed):
 *    - Maps to: DownloadComponents.SpeedComponent (from DownloadComponents.cs)  
 *    - Shows: Transfer rate "5.2 MB/s" or "--"
 *    - Sortable by current transfer rate
 * 
 * 5. STATUS COLUMN (defined here as DownloadColumns.Status):
 *    - Maps to: DownloadComponents.StatusComponent (from DownloadComponents.cs)
 *    - Contains: Progress bar + Pause/Resume + Cancel + Kebab menu
 *    - Consolidated all download actions into single column
 *    - Sortable by completion percentage (0% to 100%)
 */

/// <summary>
/// Defines column template resource keys and component keys for Downloads.
/// </summary>
public static class DownloadColumns
{
    /// <summary>Displays game name with game icon, sorted alphabetically by name.</summary>
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

    /// <summary>Displays size as "15.6 MB of 56 MB" format, sorted by total file size.</summary>
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

    /// <summary>Shows transfer rate as "5.2 MB/s" or "--" when inactive, sorted by speed.</summary>
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

    /// <summary>Contains progress bar, pause/resume, cancel buttons, and kebab menu, sorted by completion %.</summary>
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
