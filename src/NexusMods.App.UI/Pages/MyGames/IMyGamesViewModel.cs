using NexusMods.App.UI.RightContent;
using NexusMods.App.UI.WorkspaceSystem;

namespace NexusMods.App.UI.Pages.MyGames;

public interface IMyGamesViewModel : IPageViewModelInterface
{
    public IFoundGamesViewModel FoundGames { get; }

    public IFoundGamesViewModel AllGames { get; }

}
