using GameFinder.Common;
using GameFinder.StoreHandlers.Steam;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Steam'.
/// </summary>
public class SteamLocator : AGameLocator<SteamGame, int, ISteamGame>
{
    /// <inheritdoc />
    public SteamLocator(ILogger<SteamLocator> logger, AHandler<SteamGame, int> handler) : base(logger, handler)
    {
    }

    /// <inheritdoc />
    protected override IEnumerable<int> Ids(ISteamGame game) => game.SteamIds;

    /// <inheritdoc />
    protected override AbsolutePath Path(SteamGame record) => record.Path.ToAbsolutePath();
}
