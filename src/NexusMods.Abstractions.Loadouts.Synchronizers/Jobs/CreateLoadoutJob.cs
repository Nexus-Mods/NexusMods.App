using NexusMods.Abstractions.GameLocators;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A job to create a loadout
/// </summary>
public record CreateLoadoutJob(GameInstallation Installation) : IJobDefinition<Loadout.ReadOnly>;
