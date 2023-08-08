using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Abstractions.CLI;

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
        services.AddSingleton(new RegisteredVerb
        {
            Definition = T.Definition,
            Run = o => ((T)o).Delegate,
            Type = typeof(T)
        });
        return services;
    }
}
