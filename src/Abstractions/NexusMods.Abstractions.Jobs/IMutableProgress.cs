using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

/// <summary>
/// Mutable variant of <see cref="IProgress"/>.
/// </summary>
[PublicAPI]
public interface IMutableProgress : IProgress
{
    /// <summary>
    /// Setter for <see cref="IProgress.FirstStartTime"/>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="IProgress.FirstStartTime"/> has
    /// already been set.
    /// </exception>
    void SetFirstStartTime(DateTime value);

    /// <summary>
    /// Setter for <see cref="IProgress.LastStartTime"/>
    /// </summary>
    void SetLastStartTime(DateTime value);

    /// <summary>
    /// Setter for <see cref="IProgress.FinishTime"/>
    /// </summary>
    void SetFinishTime(DateTime value);

    /// <summary>
    /// Increases <see cref="IProgress.TotalDuration"/> by <paramref name="amount"/>.
    /// </summary>
    void IncreaseTotalDuration(TimeSpan amount);
}
