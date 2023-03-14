using System.Collections.ObjectModel;
using NexusMods.App.UI.Controls.GameWidget;

namespace NexusMods.App.UI.RightContent.MyGames;

public interface IMyGamesViewModel : IRightContentViewModel
{
    public IFoundGamesViewModel FoundGames
    {
        get;
    }

    public IFoundGamesViewModel AllGames { get; }

}
