using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a controllable job group.
/// </summary>
[PublicAPI]
public interface IControllableJobGroup : IJobGroup, IControllableJob;
