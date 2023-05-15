using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// A mod file that is generated on-the-fly by the application.
/// </summary>
public interface IGeneratedFile
{
    public Hash Fingerprint { get; }
    public ITriggerFilter<(ModId, ModFileId), Loadout> TriggerFilter { get; }
}
