namespace NexusMods.App.UI.RightContent.MyGames;

public interface IMyGamesViewModel : IViewModelInterface
{
    public IFoundGamesViewModel FoundGames { get; }

    public IFoundGamesViewModel AllGames { get; }

}
