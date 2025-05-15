using NexusMods.Abstractions.Jobs;
using R3;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// Synchronize the loadout with the game folder,
/// any changes in the game folder will be added to the loadout,
/// and any new changes in the loadout will be applied to the game folder.
/// </summary>
/// <param name="LoadoutId"></param>
public record SynchronizeLoadoutJob(LoadoutId LoadoutId) : IJobDefinition<Unit>;
    
