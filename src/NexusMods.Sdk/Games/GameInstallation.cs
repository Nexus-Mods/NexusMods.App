namespace NexusMods.Sdk.Games;

public record GameInstallation(GameLocatorResult LocatorResult, GameLocations Locations)
{
    /// <summary>
    /// The game found on disk.
    /// </summary>
    public IGameData Game => LocatorResult.Game;
}
