using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public record IntegrateFile : IApplyStep, IStaticFileStep
{
    public required AbsolutePath To { get; init; }
    public required Mod Mod { get; init; }
    public required Size Size { get; init; }
    public required Hash Hash { get; init; }
}