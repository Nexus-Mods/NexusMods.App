namespace NexusMods.Abstractions.Games.Stores.Origin;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// <see cref="AGame"/> managed by Origin.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="OriginGameIds"/>
/// is applied.
/// </remarks>
public interface IOriginGame : IGame
{
    /// <summary>
    /// IDs for this game used in the 'Origin' application.
    /// </summary>
    public IEnumerable<string> OriginGameIds { get; }
}
