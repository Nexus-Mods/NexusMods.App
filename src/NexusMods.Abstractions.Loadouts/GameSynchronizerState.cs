namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// The synchronization state of a game installation.
/// </summary>
public enum GameSynchronizerState
{
    /// <summary>
    /// No synchronization is currently happening.
    /// </summary>
    Idle,
    
    /// <summary>
    /// A synchronization is currently happening.
    /// </summary>
    Busy,
}
