using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;


/// <summary>
/// Specifies a new file to be added to the loadout because something else created the file in the game folders
/// </summary>
public class CreateInLoadout : IIngestStep
{
    /// <summary>
    /// The path to the file that was changed, used mostly for debugging and logging.
    /// </summary>
    public required AbsolutePath Source { get; init; }

    /// <summary>
    /// The file's new hash
    /// </summary>
    public required Hash Hash { get; init; }

    /// <summary>
    /// The file's new size
    /// </summary>
    public required Size Size { get; init; }

    /// <summary>
    /// The mod in which the file should be created
    /// </summary>
    public required ModId ModId { get; init; }

}
