using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.DataModel.TriggerFilter;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// A mod file that is generated on-the-fly by the application.
/// </summary>
public interface IGeneratedFile
{
    public ITriggerFilter<ModFilePair, Plan> TriggerFilter { get; }
    
    public Task<Hash> GenerateAsync(Stream stream, ApplyPlan plan, CancellationToken cancellationToken = default);
}
