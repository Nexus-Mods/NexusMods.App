using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.Triggers;
using NexusMods.DataModel.Loadouts.LoadoutSynchronizerDTOs;
using NexusMods.Hashing.xxHash64;

namespace NexusMods.DataModel.Loadouts.ModFiles;

/// <summary>
/// A mod file that is generated on-the-fly by the application.
/// </summary>
public interface IGeneratedFile
{
    /// <summary>
    /// The trigger filter that determines if this file should be re-generated.
    /// </summary>
    public ITriggerFilter<ModFilePair, Plan> TriggerFilter { get; }

    /// <summary>
    /// Generates the contents of the file
    /// </summary>
    /// <param name="stream"></param>
    /// <param name="plan"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Hash> GenerateAsync(Stream stream, ApplyPlan plan, CancellationToken cancellationToken = default);
}
