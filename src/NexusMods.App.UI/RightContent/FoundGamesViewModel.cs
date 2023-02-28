using Microsoft.Extensions.Logging;
using NexusMods.App.UI.ViewModels;
using NexusMods.DataModel.Games;

namespace NexusMods.App.UI.RightContent;

public class FoundGamesViewModel : AViewModel
{
    private readonly IGame[] _games;
    private readonly IGame[] _installedGames;

    public FoundGamesViewModel(ILogger<FoundGamesViewModel> logger, IEnumerable<IGame> games)
    {
        _games = games.ToArray();
        _installedGames = _games.Where(g => g.Installations.Any()).ToArray();
    }
}