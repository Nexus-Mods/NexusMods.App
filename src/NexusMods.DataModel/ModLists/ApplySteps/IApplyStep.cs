using NexusMods.Paths;

namespace NexusMods.DataModel.ModLists.ApplySteps;

public interface IApplyStep
{
    public AbsolutePath To { get; }
}