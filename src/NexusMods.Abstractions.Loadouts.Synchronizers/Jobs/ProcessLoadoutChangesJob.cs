using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Processes whether the passed loadout has changes that need to be synchronized.
/// </summary>
public record ProcessLoadoutChangesJob(LoadoutId LoadoutId) : IJobDefinition<bool>;
