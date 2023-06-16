using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns.DownloadGameName;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadGameNameViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    public string Game { get; }
}
