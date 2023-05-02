using NexusMods.DataModel.Loadouts;

namespace NexusMods.DataModel.Interprocess.Jobs;

public interface ILoadoutJob
{
    public LoadoutId LoadoutId { get; }
}
