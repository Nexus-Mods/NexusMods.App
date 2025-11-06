namespace NexusMods.Abstractions.GameLocators.Stores.GOG;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// games managed by GOG (Galaxy) launcher.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="GogIds"/>
/// is applied.
/// </remarks>
[Obsolete("Use IGameData.StoreIdentifiers instead")]
public interface IGogGame : ILocatableGame
{
    /// <summary>
    /// Returns a list of GOG game IDs.
    /// </summary>
    IEnumerable<long> GogIds { get; }

}
