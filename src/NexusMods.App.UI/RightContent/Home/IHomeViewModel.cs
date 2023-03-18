using System.Windows.Input;

namespace NexusMods.App.UI.RightContent.Home;

/// <summary>
/// View model for the Home page. Contains all the games found on the system.
/// and some notices and updates from the Nexus
/// </summary>
public interface IHomeViewModel : IRightContentViewModel
{
    /// <summary>
    /// View model for all the games found on the system.
    /// </summary>
    public IFoundGamesViewModel FoundGames { get; }

    /// <summary>
    /// Command to move to Add Game page.
    /// </summary>
    public ICommand BrowseAllGamesCommand { get; }
}
