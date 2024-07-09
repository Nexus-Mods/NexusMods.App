using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Determinate progress indicates a specific and measurable portion of a job
/// that has been completed.
/// </summary>
[PublicAPI]
public interface IDeterminateProgress : IProgress
{

}
