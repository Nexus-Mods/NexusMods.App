using GameFinder.Common;
using GameFinder.StoreHandlers.EGS;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

public class EpicLocator : AGameLocator<EGSHandler, EGSGame, string, IEpicGame>
{
    public EpicLocator(ILogger<EpicLocator> logger, AHandler<EGSGame, string> handler) : base(logger, handler)
    {
    }

    protected override IEnumerable<string> Ids(IEpicGame game) => game.EpicCatalogItemId;

    protected override AbsolutePath Path(EGSGame record) => record.InstallLocation.ToAbsolutePath();
}
