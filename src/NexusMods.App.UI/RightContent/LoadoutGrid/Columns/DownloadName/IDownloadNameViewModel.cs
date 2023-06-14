using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadNameViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    public string Game { get; }
}
