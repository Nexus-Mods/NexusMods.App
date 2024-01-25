namespace NexusMods.Abstractions.Games.Stores.GOG;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// <see cref="AGame"/> managed by GOG (Galaxy) launcher.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="GogIds"/>
/// is applied.
/// </remarks>
public interface IGogGame : IGame
{
    /// <summary>
    /// Returns a list of GOG game IDs.
    /// </summary>
    IEnumerable<long> GogIds { get; }

}
