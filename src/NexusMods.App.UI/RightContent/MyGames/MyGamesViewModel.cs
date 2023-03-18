using NexusMods.DataModel.Games;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.MyGames;

public class MyGamesViewModel : AViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    [Reactive]
    public IFoundGamesViewModel FoundGames { get; set; }

    [Reactive]
    public IFoundGamesViewModel AllGames { get; set; }

    public MyGamesViewModel(IFoundGamesViewModel foundGames, IFoundGamesViewModel allGames, IEnumerable<IGame> games)
    {
        var gamesList = games.ToList();
        FoundGames = foundGames;
        foundGames.InitializeFromFound(gamesList);

        AllGames = allGames;
        allGames.InitializeManual(gamesList);
    }
}
