using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public record RemoveFromLoadout : IApplyStep
{
    public required AbsolutePath To { get; init; }
}