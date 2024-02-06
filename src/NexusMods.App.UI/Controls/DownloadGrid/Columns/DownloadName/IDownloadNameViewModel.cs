using NexusMods.App.UI.Controls.DataGrid;
using NexusMods.App.UI.Pages.Downloads.ViewModels;

namespace NexusMods.App.UI.Controls.DownloadGrid.Columns.DownloadName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadNameViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    /// <summary>
    /// e.g. 'My Cool Mod'
    /// </summary>
    public string Name { get; }
}
