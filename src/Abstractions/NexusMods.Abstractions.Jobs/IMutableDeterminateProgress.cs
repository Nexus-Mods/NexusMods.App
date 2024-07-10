using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IDeterminateProgress"/>.
/// </summary>
[PublicAPI]
public interface IMutableDeterminateProgress : IMutableProgress, IDeterminateProgress
{
    /// <summary>
    /// Setter for <see cref="IDeterminateProgress.Percent"/>.
    /// </summary>
    void SetPercent(Percent value);

    /// <summary>
    /// Setter for <see cref="IDeterminateProgress.ProgressRate"/>.
    /// </summary>
    void SetProgressRate(ProgressRate value);

    /// <summary>
    /// Setter for <see cref="IDeterminateProgress.EstimatedFinishTime"/>
    /// </summary>
    /// <param name="value"></param>
    void SetEstimatedFinishTime(DateTime value);
}
