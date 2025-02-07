using JetBrains.Annotations;

namespace NexusMods.Abstractions.GameLocators;

/// <summary>
/// Additional store specific metadata for <see cref="GameLocatorResult"/>.
/// </summary>
[PublicAPI]
public interface IGameLocatorResultMetadata
{
    /// <summary>
    /// Converts this metadata to a format that the game locator and file hash db can use to reference a specific build, version, etc.
    /// </summary>
    public IEnumerable<string> ToLocatorIds();
}
