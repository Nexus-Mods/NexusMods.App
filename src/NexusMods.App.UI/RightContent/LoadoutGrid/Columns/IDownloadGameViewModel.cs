using NexusMods.App.UI.RightContent.Downloads.ViewModels;

namespace NexusMods.App.UI.RightContent.LoadoutGrid.Columns;

/// <summary>
/// Displays the name of a mod.
/// </summary>
public interface IDownloadGameViewModel : IColumnViewModel<IDownloadTaskViewModel>
{
    public string Game { get; }
}
