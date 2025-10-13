using NexusMods.Abstractions.GameLocators;
using NexusMods.Sdk.Jobs;

namespace NexusMods.Abstractions.Loadouts.Synchronizers;

/// <summary>
/// The specified game installation is being unmanaged and the files are being reset to their original state
/// </summary>
public record UnmanageGameJob(GameInstallation Installation) : IJobDefinition<GameInstallation>;
