using NexusMods.Hashing.xxHash64;
using NexusMods.Paths;

namespace NexusMods.DataModel.Loadouts.ApplySteps;

public interface IStaticFileStep
{
    public Hash Hash { get; }
    public Size Size { get; }
}
