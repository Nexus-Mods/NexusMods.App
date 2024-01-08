using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

/// <summary>
/// Marks a file as requiring backup.
/// </summary>
/// <remarks>
///    Used when a mod replaces an existing [game] file and the file
///    is not already backed up.
/// </remarks>
public record BackupFile : IIngestStep
{
    /// <summary>
    /// Location of the file to be backed up.
    /// </summary>
    public required AbsolutePath Source { get; init; }

    /// <summary>
    /// Hash of the file to be backed up.
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// Size of the file to be backed up.
    /// </summary>
    public required Size Size { get; init; }
}
