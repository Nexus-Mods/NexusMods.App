using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadVersion;

/// <summary>
/// Displays the version of a mod.
/// </summary>
public interface IDownloadVersionViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    /// <summary>
    /// e.g. '1.0.0'
    /// </summary>
    public string Version { get; }
}
