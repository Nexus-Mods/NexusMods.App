using GameFinder.Common;
using GameFinder.StoreHandlers.EGS;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by the Epic Games Launcher.
/// </summary>
public class EpicLocator : AGameLocator<EGSGame, string, IEpicGame>
{
    /// <inheritdoc />
    public EpicLocator(ILogger<EpicLocator> logger, AHandler<EGSGame, string> handler) : base(logger, handler)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> Ids(IEpicGame game) => game.EpicCatalogItemId;

    /// <inheritdoc />
    protected override AbsolutePath Path(EGSGame record) => record.InstallLocation.ToAbsolutePath();
}
