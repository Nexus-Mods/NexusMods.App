namespace NexusMods.App.UI.RightContent.MyGames;

public interface IMyGamesViewModel : IRightContentViewModel
{
    public IFoundGamesViewModel FoundGames
    {
        get;
    }

    public IFoundGamesViewModel AllGames { get; }

}
