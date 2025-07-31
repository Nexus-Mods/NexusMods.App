using JetBrains.Annotations;

namespace NexusMods.Abstractions.Jobs;

[PublicAPI]
public interface IJobDefinition;

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
    
    /// <summary>
    /// Indicates whether this job supports force pause via token cancellation.
    /// When true, pause operations will immediately cancel the current token,
    /// allowing interruption of external code with nested async operations.
    /// </summary>
    /// <remarks>
    /// Jobs with force pause support should handle <see cref="OperationCanceledException"/>
    /// and use <see cref="IJobContext.HandlePauseExceptionAsync"/> to distinguish between
    /// pause and true cancellation.
    /// </remarks>
    bool SupportsForcePause => false;
}
