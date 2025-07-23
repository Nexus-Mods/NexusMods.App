using GameFinder.StoreHandlers.Xbox;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by Xbox Game Pass.
/// </summary>
public class XboxLocator : AGameLocator<XboxGame, XboxGameId, IXboxGame, XboxLocator>
{
    /// <inheritdoc />
    public XboxLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.XboxGamePass;

    /// <inheritdoc />
    protected override IEnumerable<XboxGameId> Ids(IXboxGame game) => game.XboxIds.Select(XboxGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(XboxGame record) => record.Path;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(XboxGame game, IEnumerable<XboxGame> otherFoundGames)
    {
        return new XboxLocatorResultMetadata
        {
            Id = game.Id.Value,
        };
    }
}
