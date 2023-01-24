using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.CLI;

public interface IVerb
{
    public Delegate Delegate { get; }
}

public abstract class AVerb : IVerb
{
    public Delegate Delegate => Run;
    protected abstract Task<int> Run(CancellationToken token);
}

public abstract class AVerb<T> : IVerb
{
    public Delegate Delegate => Run;
    protected abstract Task<int> Run(T a, CancellationToken token);
}

public abstract class AVerb<T1, T2> : IVerb
{
    public Delegate Delegate => Run;
    protected abstract Task<int> Run(T1 a, T2 b, CancellationToken token);
}


public abstract class AVerb<T1, T2, T3> : IVerb
{
    public Delegate Delegate => Run;
    protected abstract Task<int> Run(T1 a, T2 b, T3 c, CancellationToken token);
}

public class Verb
{
    public required VerbDefinition Definition { get; init; }
    public required Func<object, Delegate> Run { get; init; }
    
    public required Type Type { get; init; }
}

public static class ServiceExtensions
{
    public static IServiceCollection AddVerb<T>(this IServiceCollection services, VerbDefinition definition) where T : IVerb
    {
        services.AddSingleton(new Verb { Definition = definition, Run = o => ((T)o).Delegate, Type = typeof(T)});
        services.AddSingleton(typeof(T));
        return services;
    }
}