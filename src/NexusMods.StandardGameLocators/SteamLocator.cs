using GameFinder.Common;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

public class SteamLocator : AGameLocator<SteamHandler, SteamGame, int, ISteamGame>
{
    public SteamLocator(ILogger<SteamLocator> logger, AHandler<SteamGame, int> handler) : base(logger, handler)
    {
    }

    protected override IEnumerable<int> Ids(ISteamGame game) => game.SteamIds;
    protected override AbsolutePath Path(SteamGame record) => record.Path.ToAbsolutePath();
}
