using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadPausePlay;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadPausePlayViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    /// <summary>
    /// Determines whether the user can pause the current download operation.
    /// </summary>
    public bool CanPause { get; set; }
}
