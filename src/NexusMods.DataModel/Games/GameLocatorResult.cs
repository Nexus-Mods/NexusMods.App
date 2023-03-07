using NexusMods.Paths;

namespace NexusMods.DataModel.Games;

/// <summary>
/// Record returned by a AGameLocator when a game is found. If the locator knows the game's version it should return
/// which will stop the rest of the system from trying to find the version by other means (such as file analysis).
/// </summary>
/// <param name="Path"></param>
/// <param name="Version"></param>
public record GameLocatorResult(AbsolutePath Path, Version? Version = null);
