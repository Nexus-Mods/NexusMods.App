using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Processes whether the passed loadout has changes that need to be synchronized.
/// </summary>
public record ProcessLoadoutChangesJob(LoadoutId LoadoutId) : IJobDefinition<bool>;
