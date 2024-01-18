using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

/// <summary>
/// Deletes a file from a game directory.
/// </summary>
/// <remarks>
///    Used when the game contains a file it shouldn't have.
///    This can be caused by an update, or a leftover from a previous
///    mod loadout.
/// </remarks>
public class DeleteFile : IApplyStep, IStaticFileStep
{
    /// <summary>
    /// Location of the file to be deleted.
    /// </summary>
    public required AbsolutePath To { get; init; }

    /// <summary>
    /// Hash of the file to be deleted [unused]
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Size of the file to be deleted [unused]
    /// </summary>
    public required Size Size { get; init; }
}
