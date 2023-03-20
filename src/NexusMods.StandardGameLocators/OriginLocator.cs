using GameFinder.Common;
using GameFinder.StoreHandlers.Origin;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Origin', EA's previous launcher.
/// </summary>
public class OriginLocator : AGameLocator<OriginGame, string, IOriginGame>
{
    /// <inheritdoc />
    public OriginLocator(ILogger<OriginLocator> logger, AHandler<OriginGame, string> handler) : base(logger, handler)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<string> Ids(IOriginGame game) => game.OriginGameIds;

    /// <inheritdoc />
    protected override AbsolutePath Path(OriginGame record) => record.InstallPath.ToAbsolutePath();
}
