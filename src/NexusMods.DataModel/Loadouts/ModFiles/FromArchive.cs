using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Loadouts.ModFiles;

[JsonName("NexusMods.DataModel.GameFiles.FromArchive")]
public record FromArchive : AStaticModFile
{
    public required HashRelativePath From { get; init; }
}
