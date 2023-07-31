using System.Windows.Input;
using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.DownloadGrid.Columns.DownloadStatus;

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

    /// <summary>
    /// Determines whether the user can pause the current download operation.
    /// </summary>
    public bool CanPause { get; set; }

    /// <summary>
    /// Pauses or resumes the current download operation.
    /// </summary>
    public ICommand PauseOrResume { get; set; }
}
