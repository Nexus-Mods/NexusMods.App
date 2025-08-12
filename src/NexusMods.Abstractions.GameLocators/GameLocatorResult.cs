using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Record returned by a AGameLocator when a game is found. If the locator knows the game's version it should return
/// which will stop the rest of the system from trying to find the version by other means (such as file analysis).
/// </summary>
/// <param name="Path">Full path to the folder which contains the game.</param>
/// <param name="GameFileSystem">Mapped filesystem of the game. This can either be the real filesystem or a WINE wrapper</param>
/// <param name="TargetOS">Target OS of the game</param>
/// <param name="Store"><see cref="GameStore"/> which installed the game.</param>
/// <param name="Metadata">Metadata about the game.</param>
/// <param name="Version">Version of the game found.</param>
public record GameLocatorResult(
    AbsolutePath Path,
    IFileSystem GameFileSystem,
    IOSInformation TargetOS,
    GameStore Store,
    IGameLocatorResultMetadata Metadata,
    Version? Version = null
);
