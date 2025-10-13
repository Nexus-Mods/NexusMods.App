using JetBrains.Annotations;

namespace NexusMods.Sdk.Jobs;

[PublicAPI]
public interface IJobDefinition
{
    /// <summary>
    /// Gets whether this job supports pausing and resuming.
    /// When false, calls to <see cref="IJobMonitor.Pause(NexusMods.Sdk.Jobs.JobId)"/> will cancel the job instead.
    /// </summary>
    bool SupportsPausing => false;

    /// <summary>
    /// Get the job state data for external access. Returns null if job has no externally visible state.
    /// </summary>
    IPublicJobStateData? GetJobStateData() => null;
}

/// <summary>
/// A typed job definition that returns a result
/// </summary>
[PublicAPI]
public interface IJobDefinition<TResultType> : IJobDefinition
    where TResultType : notnull;

/// <summary>
/// A job definition that can be started with instance method
/// </summary>
[PublicAPI]
public interface IJobDefinitionWithStart<in TParent, TResultType> : IJobDefinition<TResultType>
    where TParent : IJobDefinition<TResultType>
    where TResultType : notnull
{
    /// <summary>
    /// Starts the job
    /// </summary>
    /// <param name="context">The job context</param>
    ValueTask<TResultType> StartAsync(IJobContext<TParent> context);
}
