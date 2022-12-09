using NexusMods.DataModel.JsonConverters;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ModFiles;

[JsonName("NexusMods.DataModel.GameFiles.FromArchive")]
public record FromArchive: AStaticModFile
{
    public required HashRelativePath From { get; init; }
}