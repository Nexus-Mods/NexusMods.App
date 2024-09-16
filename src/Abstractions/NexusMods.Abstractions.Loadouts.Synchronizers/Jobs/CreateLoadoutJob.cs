using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A job to create a loadout
/// </summary>
public record CreateLoadoutJob(GameInstallation Installation) : IJobDefinition<Loadout.ReadOnly>;
