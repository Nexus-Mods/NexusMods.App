using GameFinder.Common;
using GameFinder.StoreHandlers.Origin;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

public class OriginLocator : AGameLocator<OriginHandler, OriginGame, string, IOriginGame>
{
    public OriginLocator(ILogger<OriginLocator> logger, AHandler<OriginGame, string> handler) : base(logger, GameStore.Origin, handler)
    {
    }

    protected override IEnumerable<string> Ids(IOriginGame game) => game.OriginGameIds;
    protected override AbsolutePath Path(OriginGame record) => record.InstallPath.ToAbsolutePath();
}