using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.Windows;
using NexusMods.App.UI.WorkspaceSystem;
using ReactiveUI.Fody.Helpers;

namespace NexusMods.App.UI.Pages.MyGames;

public class MyGamesDesignViewModel : APageViewModel<IMyGamesViewModel>, IMyGamesViewModel
{
    public MyGamesDesignViewModel() : base(new DesignWindowManager())
    {
        FoundGames = new FoundGamesDesignViewModel();
        AllGames = new FoundGamesDesignViewModel();
    }

    [Reactive]
    public IFoundGamesViewModel FoundGames { get; set; }

    [Reactive]
    public IFoundGamesViewModel AllGames { get; set; }
}
