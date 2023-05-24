using GameFinder.StoreHandlers.GOG;
using NexusMods.DataModel.Games;
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
}
