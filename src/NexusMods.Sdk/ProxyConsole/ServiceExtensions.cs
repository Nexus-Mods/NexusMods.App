using System.Linq.Expressions;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Sdk.ProxyConsole;

/// <summary>
/// Extension methods for <see cref="IServiceCollection"/>.
/// </summary>
[PublicAPI]
public static class ServiceExtensions
{
    /// <summary>
    /// Helper method for registering a verb.
    /// </summary>
    public static IServiceCollection AddVerb(
        this IServiceCollection serviceCollection,
        Expression<Func<Delegate>> expression)
    {
        if (expression.Body is not UnaryExpression unaryExpression)
            throw new NotSupportedException($"Expected expression body to be of type {typeof(UnaryExpression)} but received {expression.Body.GetType()}");
        if (unaryExpression.Operand is not MethodCallExpression operand)
            throw new NotSupportedException($"Expected operand to be of type {typeof(MethodCallExpression)} but received {unaryExpression.Operand.GetType()}");
        if (operand.Object is not ConstantExpression instance)
            throw new NotSupportedException($"Expected instance to be of type {typeof(ConstantExpression)} but received {operand.Object?.GetType()}");
        if (instance.Value is not MethodInfo methodInfo)
            throw new NotSupportedException($"Expected value to be of type {typeof(MethodInfo)} but received {instance.Value?.GetType()}");

        return serviceCollection.AddVerb(methodInfo);
    }

    /// <summary>
    /// Registers a method as a CLI verb.
    /// </summary>
    public static IServiceCollection AddVerb(
        this IServiceCollection serviceCollection,
        MethodInfo methodInfo)
    {
        if (methodInfo.ReturnType != typeof(Task<int>))
            throw new NotSupportedException($"Method `{methodInfo}` must return {typeof(Task<int>)}");
        if (!methodInfo.GetCustomAttributes<VerbAttribute>().TryGetFirst(out var verbAttribute))
            throw new NotSupportedException($"Method `{methodInfo}` must be marked with {typeof(VerbAttribute)}");

        var optionDefinitions = new List<OptionDefinition>();
        foreach (var parameterInfo in methodInfo.GetParameters())
        {
            var optionAttribute = parameterInfo.GetCustomAttribute<OptionAttribute>();
            var injectedAttribute = parameterInfo.GetCustomAttribute<InjectedAttribute>();

            if ((optionAttribute is null && injectedAttribute is null) || (optionAttribute is not null && injectedAttribute is not null))
                throw new NotSupportedException($"Parameter `{parameterInfo}` of method `{methodInfo}` must be marked with either {typeof(OptionAttribute)} or {typeof(InjectedAttribute)}");

            if (optionAttribute is not null)
            {
                optionDefinitions.Add(new OptionDefinition(parameterInfo.ParameterType, optionAttribute.ShortName, optionAttribute.LongName, optionAttribute.HelpText, IsInjected: false, optionAttribute.IsOptional));
                continue;
            }

            if (injectedAttribute is not null)
            {
                optionDefinitions.Add(new OptionDefinition(parameterInfo.ParameterType, ShortName: string.Empty, LongName: string.Empty, HelpText: string.Empty, IsInjected: true, IsOptional: false));
            }
        }

        return serviceCollection.AddSingleton(new VerbDefinition(verbAttribute.Name, verbAttribute.Description, methodInfo, optionDefinitions.ToArray()));
    }

    /// <summary>
    /// Adds a new module to the CLI.
    /// </summary>
    public static IServiceCollection AddModule(
        this IServiceCollection serviceCollection,
        string name,
        string description)
    {
        return serviceCollection.AddSingleton(new ModuleDefinition(name, description));
    }

    /// <summary>
    /// Adds a new module to the CLI.
    /// </summary>
    public static IServiceCollection AddModule(
        this IServiceCollection serviceCollection,
        ModuleDefinition moduleDefinition)
    {
        return serviceCollection.AddSingleton(moduleDefinition);
    }

    /// <summary>
    /// Registers a custom option parser.
    /// </summary>
    public static IServiceCollection AddOptionParser<TValue, TParser>(this IServiceCollection serviceCollection)
        where TValue : notnull
        where TParser : class, IOptionParser<TValue>
    {
        return serviceCollection.AddSingleton<IOptionParser<TValue>, TParser>();
    }

    /// <summary>
    /// Registers a custom option parser.
    /// </summary>
    public static IServiceCollection AddOptionParser<TVal>(this IServiceCollection serviceCollection, DelegateParser<TVal>.ParseDelegate parser)
        where TVal : notnull
    {
        return serviceCollection.AddSingleton<IOptionParser<TVal>>(new DelegateParser<TVal>(parser));
    }
    
    /// <summary>
    /// Registers a custom option parser.
    /// </summary>
    public static IServiceCollection AddOptionParser<TVal>(this IServiceCollection serviceCollection, Func<string, TVal> parser)
        where TVal : notnull
    {
        return serviceCollection.AddSingleton<IOptionParser<TVal>>(new DelegateParser<TVal>(s => (Value: parser(s), Error: null)));
    }

    /// <summary>
    /// Adds the default parsers.
    /// </summary>
    public static IServiceCollection AddDefaultParsers(this IServiceCollection serviceCollection)
    {
        return serviceCollection
            .AddOptionParser<bool>(s => bool.TryParse(s, out var b) ? (b, null) : (Value: false, $"Invalid boolean `{s}`"))
            .AddOptionParser<int>(s => int.TryParse(s, out var i) ? (i, null) : (Value: 0, $"Invalid integer `{i}`"))
            .AddOptionParser<string>(s => (Value: s, null));
    }
}
