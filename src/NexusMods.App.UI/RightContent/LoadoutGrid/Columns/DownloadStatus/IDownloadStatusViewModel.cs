using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadStatus;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadStatusViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    /// <summary>
    /// The text displayed in the control.
    /// </summary>
    public string Text { get; }
    
    /// <summary>
    /// Range 0 - 1.
    /// </summary>
    public float CurrentValue { get; }
    
    /// <summary>
    /// True if download is running, else false.
    /// </summary>
    public bool IsRunning { get; }
}
