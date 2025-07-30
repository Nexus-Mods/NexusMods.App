using GameFinder.StoreHandlers.Origin;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Origin;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Origin', EA's previous launcher.
/// </summary>
public class OriginLocator : AGameLocator<OriginGame, OriginGameId, IOriginGame, OriginLocator>
{
    /// <inheritdoc />
    public OriginLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.Origin;

    /// <inheritdoc />
    protected override IEnumerable<OriginGameId> Ids(IOriginGame game) => game.OriginGameIds.Select(OriginGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(OriginGame record) => record.InstallPath;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(OriginGame game, IEnumerable<OriginGame> otherFoundGames) => CreateMetadataCore(game);

    internal static IGameLocatorResultMetadata CreateMetadataCore(OriginGame game)
    {
        return new OriginLocatorResultMetadata
        {
            Id = game.Id.Value,
        };
    }
}
