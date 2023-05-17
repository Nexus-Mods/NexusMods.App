using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Adds a specific mod file to the current loadout.
/// </summary>
/// <remarks>
///    This step is used if a file is not already added
///    (not in a managed archive or already deployed).
/// </remarks>
public record AddToLoadout : IApplyStep, IStaticFileStep
{
    /// <summary>
    /// The absolute path where the mod file will be saved.
    /// </summary>
    public required AbsolutePath To { get; init; }

    /// <summary>
    /// Size of the file to be added to the loadout.
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// The hash of the file.
    /// </summary>
    public required Hash Hash { get; init; }
}
