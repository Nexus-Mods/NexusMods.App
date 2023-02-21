using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

public class GogLocator : AGameLocator<GOGHandler, GOGGame, long, IGogGame>
{
    public GogLocator(ILogger<GogLocator> logger, AHandler<GOGGame, long> handler) : base(logger, GameStore.Gog, handler)
    {
    }

    protected override IEnumerable<long> Ids(IGogGame game) => game.GogIds;
    protected override AbsolutePath Path(GOGGame record) => record.Path.ToAbsolutePath();
}