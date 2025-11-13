using NexusMods.Sdk.Games;
using NexusMods.Sdk.Jobs;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// A job to create a loadout
/// </summary>
public record CreateLoadoutJob(GameInstallation Installation) : IJobDefinition<Loadout.ReadOnly>;
