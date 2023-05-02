using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

public interface IModJob : ILoadoutJob
{
    public ModId ModId { get; }
}
