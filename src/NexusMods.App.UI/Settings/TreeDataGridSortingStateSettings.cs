using System.ComponentModel;
using NexusMods.Sdk.Settings;

namespace NexusMods.App.UI.Settings;

public record TreeDataGridSortingStateSettings : ISettings
{
    public string? SortedColumnKey { get; set; } = null;
    
    public ListSortDirection? SortDirection { get; set; } = null;

    /// <summary>
    /// Used to invalidate previously stored settings (e.g. columns changed, new defaults etc)
    /// </summary>
    public uint SchemaRevision { get; init; } = 0;
    
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder;
    }
}
