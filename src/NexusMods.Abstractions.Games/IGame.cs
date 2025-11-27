using DynamicData.Kernel;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.Library.Installers;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Sdk.Games;

namespace NexusMods.Abstractions.Games;

/// <summary>
/// Interface for a specific game recognized by the app. A single game can have
/// multiple installations.
/// </summary>
public interface IGame : IGameData
{
    /// <summary>
    /// Gets all available installers this game supports.
    /// </summary>
    ILibraryItemInstaller[] LibraryItemInstallers { get; }

    /// <summary>
    /// An array of all instances of <see cref="IDiagnosticEmitter"/> supported
    /// by the game.
    /// </summary>
    IDiagnosticEmitter[] DiagnosticEmitters { get; }

    /// <summary>
    /// The synchronizer for this game.
    /// </summary>
    ILoadoutSynchronizer Synchronizer { get; }
    
    /// <summary>
    /// The sort order manager for this game.
    /// </summary>
    ISortOrderManager SortOrderManager { get; }
}
