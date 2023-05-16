using GameFinder.StoreHandlers.EGS;
using NexusMods.DataModel.Games;
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
}
