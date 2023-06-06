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
    /// <inheritdoc />
    public required Size Size { get; init; }

    /// <inheritdoc />
    public required Hash Hash { get; init; }

    /// <inheritdoc />
    public required GamePath To { get; init; }
}
