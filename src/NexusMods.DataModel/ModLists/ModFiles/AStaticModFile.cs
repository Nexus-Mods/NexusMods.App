using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ModFiles;

public abstract record AStaticModFile : AModFile
{
    public required Hash Hash { get; init; }
    public required Size Size { get; init; }
}