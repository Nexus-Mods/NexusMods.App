using GameFinder.StoreHandlers.EGS;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EGS;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by the Epic Games Launcher.
/// </summary>
public class EpicLocator : AGameLocator<EGSGame, EGSGameId, IEpicGame, EpicLocator>
{
    /// <inheritdoc />
    public EpicLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.EGS;

    /// <inheritdoc />
    protected override IEnumerable<EGSGameId> Ids(IEpicGame game) => game.EpicCatalogItemId.Select(EGSGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(EGSGame record) => record.InstallLocation;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(EGSGame game, IEnumerable<EGSGame> otherFoundGames) => CreateMetadataCore(game);

    internal static IGameLocatorResultMetadata CreateMetadataCore(EGSGame game)
    {
        return new EpicLocatorResultMetadata
        {
            CatalogItemId = game.CatalogItemId.Value,
            ManifestHashes = game.ManifestHash,
        };
    }
}
