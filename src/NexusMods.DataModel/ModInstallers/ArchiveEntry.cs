using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.ModInstallers;

public record ArchiveEntry
{
    public required RelativePath Path { get; init; }
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
}
