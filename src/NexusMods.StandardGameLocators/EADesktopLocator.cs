using GameFinder.StoreHandlers.EADesktop;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.EADesktop;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by the 'EA Desktop' application, the successor to Origin.
/// </summary>
// ReSharper disable once InconsistentNaming
public class EADesktopLocator : AGameLocator<EADesktopGame, EADesktopGameId, IEADesktopGame, EADesktopLocator>
{
    /// <summary/>
    public EADesktopLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.EADesktop;

    /// <inheritdoc />
    protected override IEnumerable<EADesktopGameId> Ids(IEADesktopGame game) => game.EADesktopSoftwareIDs.Select(EADesktopGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(EADesktopGame record) => record.BaseInstallPath;

    /// <inheritdoc />
    protected override IGameLocatorResultMetadata CreateMetadata(EADesktopGame game, IEnumerable<EADesktopGame> otherFoundGames)
    {
        return new EADesktopLocatorResultMetadata
        {
            SoftwareId = game.EADesktopGameId.Value
        };
    }
}
