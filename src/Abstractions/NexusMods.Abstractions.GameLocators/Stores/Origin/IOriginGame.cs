namespace NexusMods.Abstractions.GameLocators.Stores.Origin;

/// <summary>
/// When implemented, enables support for detecting installations of your
/// games managed by Origin.
/// </summary>
/// <remarks>
/// Game detection is automatic provided the correct <see cref="OriginGameIds"/>
/// is applied.
/// </remarks>
public interface IOriginGame : ILocatableGame
{
    /// <summary>
    /// IDs for this game used in the 'Origin' application.
    /// </summary>
    public IEnumerable<string> OriginGameIds { get; }
}
