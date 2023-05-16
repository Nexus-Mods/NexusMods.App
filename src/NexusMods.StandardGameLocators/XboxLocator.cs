using GameFinder.StoreHandlers.Xbox;
using NexusMods.DataModel.Games;
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
}
