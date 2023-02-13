using Microsoft.Extensions.DependencyInjection;
// ReSharper disable InconsistentNaming

namespace NexusMods.CLI;

/// <summary>
/// Represents an individual action, e.g. 'Analyze Game'
/// </summary>
public class Verb
{
    /// <summary>
    /// Describes the verb; its name, description and options.
    /// </summary>
    public required VerbDefinition Definition { get; init; }
    
    /// <summary>
    /// The function that is ran to execute it.
    /// </summary>
    public required Func<object, Delegate> Run { get; init; }

    /// <summary>
    /// Generic type of the class used.
    /// </summary>
    public required Type Type { get; init; }
}

public interface IVerb
{
    public Delegate Delegate { get; }
    static abstract VerbDefinition Definition { get; }
}

public interface AVerb : IVerb
{
    Delegate IVerb.Delegate => Run;
    public Task<int> Run(CancellationToken token);
}

public interface AVerb<in T> : IVerb
{
    Delegate IVerb.Delegate => Run;
    public Task<int> Run(T a, CancellationToken token);
}

public interface AVerb<in T1, in T2> : IVerb
{
    Delegate IVerb.Delegate => Run;
    public Task<int> Run(T1 a, T2 b, CancellationToken token);
}

public interface AVerb<in T1, in T2, in T3> : IVerb
{
    Delegate IVerb.Delegate => Run;
    public Task<int> Run(T1 a, T2 b, T3 c, CancellationToken token);
}

public record VerbDefinition(string Name, string Description, OptionDefinition[] Options);

public static class ServiceExtensions
{
    public static IServiceCollection AddVerb<T>(this IServiceCollection services) where T : IVerb
    {
        services.AddSingleton(new Verb
        {
            Definition = T.Definition, 
            Run = o => ((T)o).Delegate, 
            Type = typeof(T)
        });
        
        services.AddSingleton(typeof(T));
        return services;
    }
}