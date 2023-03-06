using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public record AddToLoadout : IApplyStep, IStaticFileStep
{
    public required AbsolutePath To { get; init; }
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
    public required ModId Mod { get; init; }
}
