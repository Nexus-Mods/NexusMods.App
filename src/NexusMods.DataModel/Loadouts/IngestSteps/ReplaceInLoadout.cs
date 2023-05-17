using NexusMods.DataModel.Loadouts.ApplySteps;
using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.IngestSteps;

public record ReplaceInLoadout : IIngestStep
{
    public required AbsolutePath To { get; init; }
    public required Hash Hash { get; init; }
    
    public required Size Size { get; init; }
    
    public required ModFileId ModFileId { get; init; }
    
    public required ModId ModId { get; init; }
}
