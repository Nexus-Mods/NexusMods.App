using GameFinder.StoreHandlers.GOG;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.GOG;
using NexusMods.Abstractions.Games;
using NexusMods.Paths;

namespace NexusMods.StandardGameLocators;

/// <summary>
/// Finds games managed by 'Good Old Games Galaxy' (GOG Galaxy) application.
/// </summary>
public class GogLocator : AGameLocator<GOGGame, GOGGameId, IGogGame, GogLocator>
{
    /// <inheritdoc />
    public GogLocator(IServiceProvider provider) : base(provider) { }

    /// <inheritdoc />
    protected override GameStore Store => GameStore.GOG;

    /// <inheritdoc />
    protected override IEnumerable<GOGGameId> Ids(IGogGame game) => game.GogIds.Select(GOGGameId.From);

    /// <inheritdoc />
    protected override AbsolutePath Path(GOGGame record) => record.Path;

    /// <inheritdoc/>
    protected override IGameLocatorResultMetadata CreateMetadata(GOGGame game, IEnumerable<GOGGame> otherFoundGames) => CreateMetadataCore(game, otherFoundGames);

    internal static IGameLocatorResultMetadata CreateMetadataCore(GOGGame game, IEnumerable<GOGGame> otherFoundGames)
    {
        var dlcIds = otherFoundGames
            .Where(g => g.ParentGameId == game.Id)
            .Select(g => (ulong)g.Id.Value)
            .ToArray();
        
        return new GOGLocatorResultMetadata
        {
            Id = game.Id.Value,
            BuildId = game.BuildId,
            DLCBuildIds = dlcIds,
        };
    }
}
