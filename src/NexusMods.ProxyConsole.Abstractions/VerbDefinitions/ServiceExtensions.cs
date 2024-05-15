using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

/// <summary>
/// Service extensions for verb definitions.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Helper method for registering a verb via .AddVerb(() => SomeClass.SomeMethod);
    /// </summary>
    /// <param name="coll"></param>
    /// <param name="fn"></param>
    /// <returns></returns>
    public static IServiceCollection AddVerb(this IServiceCollection coll, Expression<Func<Delegate>> fn)
    {
        var methodInfo = (MethodInfo)((ConstantExpression)((MethodCallExpression)((UnaryExpression)fn.Body).Operand).Object!)
            ?.Value!;
        return coll.AddVerb(methodInfo);
    }


    /// <summary>
    /// Registers a method as a CLI verb. The method must be static and have a return type of
    /// Task&lt;int&gt;.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="info"></param>
    /// <returns></returns>
    public static IServiceCollection AddVerb(this IServiceCollection services, MethodInfo info)
    {
        if (info.ReturnType != typeof(Task<int>))
        {
            throw new ArgumentException("Method must return a Task<int>", nameof(info));
        }

        var verbDefinition = info.GetCustomAttributes()
            .OfType<VerbAttribute>()
            .FirstOrDefault();
        if (verbDefinition is null)
        {
            throw new ArgumentException("Method must be marked with a VerbAttribute", nameof(info));
        }

        var options = new List<OptionDefinition>();
        foreach (var param in info.GetParameters())
        {
            var option = param.GetCustomAttribute<OptionAttribute>();
            var injected = param.GetCustomAttribute<InjectedAttribute>();
            if (option is null && injected is null)
            {
                throw new ArgumentException(
                    "Method parameters must be marked with either an OptionAttribute or an InjectedAttribute",
                    nameof(info));
            }

            if (option is not null && injected is not null)
            {
                throw new ArgumentException(
                    "Method parameters cannot be marked with both an OptionAttribute and an InjectedAttribute",
                    nameof(info));
            }

            if (option is not null)
            {
                options.Add(new OptionDefinition(param.ParameterType, option.ShortName, option.LongName, option.HelpText, false));
            }
            else if (injected is not null)
            {
                options.Add(new OptionDefinition(param.ParameterType, string.Empty, string.Empty, string.Empty, true));
            }
        }

        return services.AddSingleton(_ => new VerbDefinition(verbDefinition.Name, verbDefinition!.Description, info, options.ToArray()));

    }

    /// <summary>
    /// Adds a custom option parser for a given type.
    /// </summary>
    /// <param name="collection"></param>
    /// <typeparam name="TVal"></typeparam>
    /// <typeparam name="TParser"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddOptionParser<TVal, TParser>(this IServiceCollection collection)
        where TParser : class, IOptionParser<TVal>
    {
        return collection.AddSingleton<IOptionParser<TVal>, TParser>();
    }

    /// <summary>
    /// Registers a custom option parser for a given type.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="parser"></param>
    /// <typeparam name="TVal"></typeparam>
    /// <returns></returns>
    public static IServiceCollection AddOptionParser<TVal>(this IServiceCollection services, Func<string, (TVal? Value, string? Error)> parser)
    {
        return services.AddSingleton<IOptionParser<TVal>>(new DelegateParser<TVal>(parser));
    }

    /// <summary>
    /// Adds the default parsers for boolean, integer and string.
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static IServiceCollection AddDefaultParsers(this IServiceCollection collection)
    {
        collection.AddOptionParser<bool>(s => bool.TryParse("true", out var b) ? (b, null) : (default, "Invalid boolean"));
        collection.AddOptionParser<int>(s => int.TryParse(s, out var i) ? (i, null) : (default, "Invalid integer"));
        collection.AddOptionParser<string>(s => (s, null));
        return collection;
    }
}
