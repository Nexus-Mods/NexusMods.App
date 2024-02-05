using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadSize;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadSizeViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    public string Size { get; }
}
