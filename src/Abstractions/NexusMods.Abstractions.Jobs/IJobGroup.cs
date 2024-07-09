using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Represents a job of <see cref="IJob"/>.
/// </summary>
[PublicAPI]
public interface IJobGroup : IJob, IReadOnlyList<IJob>;
