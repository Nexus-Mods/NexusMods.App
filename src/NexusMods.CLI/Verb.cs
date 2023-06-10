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

/// <summary>
/// Represents an individual action, e.g. 'Analyze Game'
/// </summary>
public interface IVerb
{
    /// <summary>
    /// The function that is ran to execute it.
    /// </summary>
    public Delegate Delegate { get; }
    
    /// <summary>
    /// Describes the verb; its name, description and options.
    /// </summary>
    static abstract VerbDefinition Definition { get; }
}

/// <summary>
/// Abstract class for a verb
/// </summary>
public interface AVerb : IVerb
{
    Delegate IVerb.Delegate => Run;
    
    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes a single argument
/// </summary>
/// <typeparam name="T"></typeparam>
public interface AVerb<in T> : IVerb
{
    Delegate IVerb.Delegate => Run;
    
    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T a, CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes two arguments
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
public interface AVerb<in T1, in T2> : IVerb
{
    Delegate IVerb.Delegate => Run;
    
    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T1 a, T2 b, CancellationToken token);
}

/// <summary>
/// Abstract class for a verb that takes three arguments
/// </summary>
/// <typeparam name="T1"></typeparam>
/// <typeparam name="T2"></typeparam>
/// <typeparam name="T3"></typeparam>
public interface AVerb<in T1, in T2, in T3> : IVerb
{
    Delegate IVerb.Delegate => Run;
    
    /// <summary>
    /// Runs the verb
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <param name="c"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public Task<int> Run(T1 a, T2 b, T3 c, CancellationToken token);
}

/// <summary>
/// The definition of a verb, its name, description and options.
/// </summary>
/// <param name="Name"></param>
/// <param name="Description"></param>
/// <param name="Options"></param>
public record VerbDefinition(string Name, string Description, OptionDefinition[] Options);

/// <summary>
/// Extension methods for the <see cref="IServiceCollection"/> class.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Adds a verb to the service collection
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
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
