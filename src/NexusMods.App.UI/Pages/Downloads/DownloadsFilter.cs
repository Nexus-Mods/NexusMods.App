using DynamicData.Kernel;
using NexusMods.Abstractions.NexusWebApi.Types.V2;

namespace NexusMods.App.UI.Pages.Downloads;

/// <summary>
/// Filter for downloads based on scope and optional game ID.
/// </summary>
/// <param name="Scope">The filtering scope to apply</param>
/// <param name="GameId">Optional game ID for game-specific filtering</param>
public readonly record struct DownloadsFilter(DownloadsScope Scope, Optional<GameId> GameId)
{
    /// <summary>
    /// Creates a filter for all downloads.
    /// </summary>
    public static DownloadsFilter All() => new(DownloadsScope.All, Optional<GameId>.None);

    /// <summary>
    /// Creates a filter for active downloads only.
    /// </summary>
    public static DownloadsFilter Active() => new(DownloadsScope.Active, Optional<GameId>.None);

    /// <summary>
    /// Creates a filter for completed downloads only.
    /// </summary>
    public static DownloadsFilter Completed() => new(DownloadsScope.Completed, Optional<GameId>.None);

    /// <summary>
    /// Creates a filter for downloads from a specific game.
    /// </summary>
    public static DownloadsFilter ForGame(GameId gameId) => new(DownloadsScope.GameSpecific, gameId);
}

/// <summary>
/// Represents the scope for filtering downloads.
/// </summary>
public enum DownloadsScope : byte
{
    /// <summary>
    /// Show all downloads regardless of status.
    /// </summary>
    All = 0,
    
    /// <summary>
    /// Show only active downloads (Running or Paused).
    /// </summary>
    Active = 1,
    
    /// <summary>
    /// Show only completed downloads.
    /// </summary>
    Completed = 2,
    
    /// <summary>
    /// Show downloads for a specific game.
    /// </summary>
    GameSpecific = 3,
}
