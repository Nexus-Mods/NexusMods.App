using NexusMods.App.UI.ViewModels;
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
        FoundGames = foundGames;
        foundGames.InitializeFromFound(games);

        AllGames = allGames;
        allGames.InitializeManual(games);
    }
}
