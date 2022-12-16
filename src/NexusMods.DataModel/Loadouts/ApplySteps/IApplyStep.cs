using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public interface IApplyStep
{
    public AbsolutePath To { get; }
}