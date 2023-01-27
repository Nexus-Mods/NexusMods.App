using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

public class EADesktopLocator : AGameLocator<EADesktopHandler, EADesktopGame, string, IEADesktopGame>
{
    public EADesktopLocator(ILogger<EADesktopLocator> logger, AHandler<EADesktopGame, string> handler) : base(logger, handler)
    {
    }

    protected override IEnumerable<string> Ids(IEADesktopGame game) => game.EADesktopSoftwareIDs;
    protected override AbsolutePath Path(EADesktopGame record) => record.BaseInstallPath.ToAbsolutePath();
}