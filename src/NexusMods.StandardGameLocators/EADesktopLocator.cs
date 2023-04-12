using GameFinder.Common;
using GameFinder.StoreHandlers.EADesktop;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by the 'EA Desktop' application, the successor to Origin.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EADesktopLocator : AGameLocator<EADesktopGame, string, IEADesktopGame>
{
    /// <summary/>
    public EADesktopLocator(ILogger<EADesktopLocator> logger, AHandler<EADesktopGame, string> handler) : base(logger, handler)
    {
    }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.EADesktop;

    /// <inheritdoc />
    protected override IEnumerable<string> Ids(IEADesktopGame game) => game.EADesktopSoftwareIDs;

    /// <inheritdoc />
    protected override AbsolutePath Path(EADesktopGame record) => record.BaseInstallPath.ToAbsolutePath();
}
