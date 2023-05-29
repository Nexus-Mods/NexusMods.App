using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Denotes any file which is originally sourced from an archive for installation.
/// </summary>
[JsonName("NexusMods.DataModel.GameFiles.FromArchive")]
public record FromArchive : AModFile, IFromArchive, IToFile
{
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
    public required GamePath To { get; init; }
}
