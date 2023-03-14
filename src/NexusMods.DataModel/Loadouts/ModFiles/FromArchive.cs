using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// Denotes any file which is originally sourced from an archive for installation.
/// </summary>
[JsonName("NexusMods.DataModel.GameFiles.FromArchive")]
public record FromArchive : AStaticModFile
{
    /// <summary>
    /// A tuple which contains the hash of the source archive a file has came from and its
    /// relative path.
    /// </summary>
    public required HashRelativePath From { get; init; }
}
