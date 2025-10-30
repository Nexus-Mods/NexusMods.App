
using NexusMods.App.UI.Settings;

namespace NexusMods.App.UI.Controls;

public record TreeDataGridSortingOptions
{
    public required bool UseSortingStatePersistence { get; init; } = false;
    public required string SettingsScopeKey { get; init; } 
    public TreeDataGridSortingStateSettings? DefaultSortingState { get; init; } = null;


    public static TreeDataGridSortingOptions DefaultOptions { get; } = new()
    {
        UseSortingStatePersistence = false,
        SettingsScopeKey = "",
    };
}
