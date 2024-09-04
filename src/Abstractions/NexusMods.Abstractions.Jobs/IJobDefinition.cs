namespace NexusMods.Abstractions.Jobs;

public interface IJobDefinition
{
    
}

/// <summary>
/// A typed job definition that returns a result
/// </summary>
public interface IJobDefinition<TResultType> : IJobDefinition;


/// <summary>
/// A job definition that can be started with instance method
/// </summary>
/// <typeparam name="TParent"></typeparam>
/// <typeparam name="TResultType"></typeparam>
public interface IJobDefinitionWithStart<TParent, TResultType> : IJobDefinition<TResultType> 
    where TParent : IJobDefinition<TResultType>
{
    /// <summary>
    /// Starts the job
    /// </summary>
    /// <param name="context">The job context</param>
    ValueTask<TResultType> StartAsync(IJobContext<TParent> context);
}
