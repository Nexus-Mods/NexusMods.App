using Microsoft.Extensions.DependencyInjection;
using NexusMods.CrossPlatform.Process;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform.Console;

/// <summary>
/// Abstraction for spawning a console window with logging.
/// </summary>
// ReSharper disable once InconsistentNaming
public interface IConsoleSpawner
{
    /// <summary>
    /// Spawns the console (if possible).
    /// </summary>
    /// <returns>True on success, False on fail.</returns>
    bool Spawn();

    /// <summary>
    /// Runs the appropriate spawn implementation for the current console.
    /// </summary>
    static bool SpawnForCurrentOS()
    {
        return OSInformation.Shared.MatchPlatform(
            onWindows: () => new ConsoleSpawnerWindows().Spawn(),
            onLinux: () => new DummyConsoleSpawner().Spawn(),
            onOSX: () => new DummyConsoleSpawner().Spawn()
        );
    }
}
