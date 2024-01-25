using GameFinder.StoreHandlers.Xbox;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Games.Stores.Xbox;
using NexusMods.Abstractions.Installers.DTO;
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
    protected override IGameLocatorResultMetadata CreateMetadata(XboxGame game)
    {
        return new XboxLocatorResultMetadata
        {
            Id = game.Id.Value,
        };
    }
}
