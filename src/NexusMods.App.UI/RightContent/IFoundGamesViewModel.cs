using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;
using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.RightContent;

public interface IFoundGamesViewModel : IRightContentViewModel
{
    /// <summary>
    /// All the games in this list.
    /// </summary>
    public ReadOnlyObservableCollection<IGameWidgetViewModel> Games { get; }

    /// <summary>
    /// Initialize the list of games in a way where each "add" button would manage
    /// an already found game.
    /// </summary>
    /// <param name="games"></param>
    /// <returns></returns>
    void InitializeFromFound(IEnumerable<IGame> games);

    /// <summary>
    /// Initialize the list of games in a way where each "add" button would
    /// trigger a manual add of the game.
    /// </summary>
    /// <param name="games"></param>
    /// <returns></returns>
    void InitializeManual(IEnumerable<IGame> games);
}
