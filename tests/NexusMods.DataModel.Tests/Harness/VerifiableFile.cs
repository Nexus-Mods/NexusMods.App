using NexusMods.Abstractions.Loadouts.Synchronizers;

namespace NexusMods.DataModel.Tests.Harness;

public record VerifiableFile
{
    public required string GamePath { get; init; }
    public required ulong Size { get; init; }
    
    public required ulong Hash { get; init; }
    
    public required FileChangeType ChangeType { get; init; }
    
    public static VerifiableFile From(DiskDiffEntry diskDiffEntry)
    {
        return new VerifiableFile
        {
            GamePath = diskDiffEntry.GamePath.ToString(),
            Size = diskDiffEntry.Size.Value,
            Hash = diskDiffEntry.Hash.Value,
            ChangeType = diskDiffEntry.ChangeType,
        };
    }
}
