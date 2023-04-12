using GameFinder.Common;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Good Old Games Galaxy' (GOG Galaxy) application.
/// </summary>
public class GogLocator : AGameLocator<GOGGame, long, IGogGame>
{
    /// <inheritdoc />
    public GogLocator(ILogger<GogLocator> logger, AHandler<GOGGame, long> handler) : base(logger, handler)
    {
    }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.GOG;

    /// <inheritdoc />
    protected override IEnumerable<long> Ids(IGogGame game) => game.GogIds;

    /// <inheritdoc />
    protected override AbsolutePath Path(GOGGame record) => record.Path.ToAbsolutePath();
}
