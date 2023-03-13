using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Represents an operation done on a static [non-moving] file in the game directory.
/// </summary>
public interface IStaticFileStep
{
    /// <summary>
    /// Hash of the file.
    /// </summary>
    public Hash Hash { get; }

    /// <summary>
    /// Size of the file in bytes
    /// </summary>
    public Size Size { get; }
}
