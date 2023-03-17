using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.RightContent.MyGames;

public class MyGamesDesignViewModel : AViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    public MyGamesDesignViewModel()
    {
        FoundGames = new FoundGamesDesignViewModel();
        AllGames = new FoundGamesDesignViewModel();
    }

    [Reactive]
    public IFoundGamesViewModel FoundGames { get; set; }

    [Reactive]
    public IFoundGamesViewModel AllGames { get; set; }
}
