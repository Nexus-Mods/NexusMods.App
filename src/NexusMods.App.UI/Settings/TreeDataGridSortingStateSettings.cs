using System.ComponentModel;
using NexusMods.Sdk.Settings;

namespace NexusMods.App.UI.Settings;

public record TreeDataGridSortingStateSettings : ISettings
{
    public string? SortedColumnKey { get; set; } = null;
    
    public ListSortDirection? SortDirection { get; set; } = null;
    
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
