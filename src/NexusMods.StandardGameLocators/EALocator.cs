using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

public class EALocator : AGameLocator<EADesktopHandler, EADesktopGame, string, IEAGame>
{
    public EALocator(ILogger<EALocator> logger, AHandler<EADesktopGame, string> handler) : base(logger, handler)
    {
    }

    protected override IEnumerable<string> Ids(IEAGame game) => game.EASoftwareIDs;
    protected override AbsolutePath Path(EADesktopGame record) => record.BaseInstallPath.ToAbsolutePath();
}