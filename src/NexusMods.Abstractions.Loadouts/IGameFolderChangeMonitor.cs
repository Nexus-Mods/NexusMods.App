using NexusMods.Abstractions.GameLocators;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Loadouts;

/// <summary>
/// Monitors game installation folders for file changes and emits a signal
/// when changes settle for a debounce period.
/// </summary>
public interface IGameFolderChangeMonitor
{
    /// <summary>
    /// Observable that emits a <see cref="GameInstallation"/> when file changes
    /// for that installation have settled for the configured debounce window.
    /// </summary>
    IObservable<GameInstallation> ChangesSettled { get; }

    /// <summary>
    /// Start monitoring relevant game folders.
    /// </summary>
    void Start();
}

