namespace NexusMods.DataModel.Games.GameCapabilities;

/// <summary>
///     The base interface for all game capabilities abstract classes.
///
///     GameCapabilities represent optional functionalities that a game can support,
///     such as a particular mod installer or the use of a plugin system.
///
///     Games can have game-specific implementations of these capabilities,
///     allowing for custom logic to be used for each game.
///
///     This interface should not be implemented directly,
///     but rather through one of the abstract classes
///     defining a specific capability type.
/// </summary>
public interface IGameCapability
{
    /// <summary>
    /// The unique identifier for this capability type.
    /// </summary>
    /// <remarks>
    /// This should only be implemented by the abstract classes inheriting directly from <see cref="IGameCapability"/>
    /// and then be sealed.
    /// </remarks>
    public static GameCapabilityId CapabilityId { get; }
}
