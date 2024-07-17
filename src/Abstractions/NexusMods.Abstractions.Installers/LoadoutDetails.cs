using JetBrains.Annotations;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;

namespace NexusMods.Abstractions.Installers;

/// <summary>
/// Provides details about a loaodut.
/// </summary>
[PublicAPI]
public record LoadoutDetails
{
    /// <summary>
    /// Gets the loadout.
    /// </summary>
    public required Loadout.ReadOnly Loadout { get; init; }

    /// <summary>
    /// Gets the game installation.
    /// </summary>
    public GameInstallation GameInstallation => Loadout.InstallationInstance;
}
