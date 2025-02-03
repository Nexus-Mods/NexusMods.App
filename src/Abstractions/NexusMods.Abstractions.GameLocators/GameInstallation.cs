using NexusMods.Abstractions.GameLocators.GameCapabilities;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Paths;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Defines an individual installation of a game, i.e. a unique combination of
/// Version and Location.
/// </summary>
public class GameInstallation : IEquatable<GameInstallation>
{
    /// <summary>
    /// Empty game installation, used for testing and some cases where a property must be set.
    /// </summary>
    public static GameInstallation Empty => new();
    
    /// <summary>
    /// Contains the manual install destinations for AdvancedInstaller and friends.
    /// </summary>
    public List<IModInstallDestination> InstallDestinations { get; init; } = new();

    /// <summary>
    /// The location on-disk of this game and it's associated paths [e.g. Saves].
    /// </summary>
    public IGameLocationsRegister LocationsRegister { get; init; } = null!;

    /// <summary>
    /// The game to which this installation belongs.
    /// </summary>
    public ILocatableGame Game { get; init; } = null!;

    /// <summary>
    /// The <see cref="GameStore"/> which was used to install the game.
    /// </summary>
    public GameStore Store { get; init; } = GameStore.Unknown;

    /// <summary>
    /// Gets the metadata returned by the game locator.
    /// </summary>
    public IGameLocatorResultMetadata? LocatorResultMetadata { get; init; }

    /// <summary>
    /// Converts a <see cref="AbsolutePath"/> to a <see cref="GamePath"/> assuming the absolutePath exists under a game location.
    /// </summary>
    /// <param name="absolutePath">The absolutePath to convert.</param>
    /// <returns>Path to the game.</returns>
    public GamePath ToGamePath(AbsolutePath absolutePath)
    {
        return LocationsRegister.ToGamePath(absolutePath);
    }

    /// <summary>
    /// Utility method used to determine whether <see cref="Game"/> can
    /// be casted to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to cast the game to.</typeparam>
    /// <returns>True if the cast is possible, else false.</returns>
    public bool Is<T>() where T : ILocatableGame => Game is T;

    /// <summary>
    /// The <see cref="IGameLocator"/> that found this installation.
    /// </summary>
    public IGameLocator Locator { get; init; } = null!;
    
    /// <summary>
    /// An entity id that points to the game metadata in the MnemonicDB instance
    /// </summary>
    public EntityId GameMetadataId { get; init; }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is not GameInstallation other)
            return false;

        return GameMetadataId == other.GameMetadataId;
    }

    /// <inheritdoc />
    public bool Equals(GameInstallation? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return GameMetadataId.Equals(other.GameMetadataId);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return GameMetadataId.GetHashCode();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Game.Name} {GameMetadataId} ({Store.Value})";
    }
}
