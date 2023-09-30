using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.CLI;

namespace NexusMods.CLI;

/// <summary>
/// Generates the command line parsing structures, and connects them to verbs
/// </summary>
public class CommandLineConfigurator
{
    private static IServiceProvider _provider = null!;
    private readonly IEnumerable<RegisteredVerb> _verbs;
    private readonly MethodInfo _getOptionMethod;
    private readonly IRenderer[] _renderers;
    private readonly IRenderer _defaultConsoleRenderer;

    /// <summary/>
    /// <param name="verbs">
    ///     List of supported verbs.
    ///     This is populated by DI; multiple registrations using to <see cref="IServiceCollection"/> is resolved as
    ///     an enumerable of verbs.
    /// </param>
    /// <param name="provider">Instance of dependency injection container.</param>
    /// <param name="selector"></param>
    /// <param name="renderers"></param>
    public CommandLineConfigurator(IEnumerable<RegisteredVerb> verbs, IServiceProvider provider, CliGuidedInstaller selector, IEnumerable<IRenderer> renderers)
    {
        _renderers = renderers.ToArray();
        _defaultConsoleRenderer = _renderers.FirstOrDefault(r => r.Name == "console") ?? _renderers.First();
        _provider = provider;
        _verbs = verbs.ToArray();
        _getOptionMethod = typeof(CommandLineConfigurator).GetMethod(nameof(GetOption), BindingFlags.Instance | BindingFlags.NonPublic)!;
        selector.Renderer = _defaultConsoleRenderer;
    }

    /// <summary>
    /// Creates the main verb-less root command the application executes.
    /// </summary>
    public RootCommand MakeRoot()
    {
        var root = new RootCommand();
        var renderOption = new Option<IRenderer>("--renderer", parseArgument:
            x =>
            {
                var found = _provider.GetServices<IRenderer>().FirstOrDefault(r => r.Name == x.Tokens.Single().Value);
                if (found == null)
                    throw new Exception($"Invalid renderer {x.Tokens.Single()}");

                return found;
            });

        root.AddOption(renderOption);
        root.AddOption(new Option<bool>("--noBanner"));

        foreach (var verb in _verbs)
            root.Add(MakeCommand(verb.Type, verb.Run, verb.Definition));

        return root;
    }

    private Command MakeCommand(Type verbType, Func<object, Delegate> verbHandler, VerbDefinition definition)
    {
        var command = new Command(definition.Name, definition.Description);

        foreach (var option in definition.Options)
        {
            var optionInstance = (Option)_getOptionMethod.MakeGenericMethod(option.ReturnType)
                .Invoke(this, new object[] { _provider, option })!;
            command.Add(optionInstance);
        }

        command.Handler = new HandlerDelegate(_provider, verbType, verbHandler, _renderers, _defaultConsoleRenderer);
        return command;
    }

    /// <summary>
    /// Converts a <see cref="OptionDefinition{T}"/> into a <see cref="Option{T}"/>
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="definition"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    // ReSharper disable once UnusedMember.Local
    private Option GetOption<T>(IServiceProvider provider, OptionDefinition<T> definition)
    {
        var converter = provider.GetService<IOptionParser<T>>();

        var aliases = new[] { "-" + definition.ShortOption, "--" + definition.LongOption };
        if (converter == null)
            return new Option<T>(aliases, description: definition.Description);

        var opt = new Option<T>(aliases, description: definition.Description,
            parseArgument: x => converter.Parse(x.Tokens.Single().Value, definition));

        opt.AddCompletions(x => converter.GetOptions(x.WordToComplete));
        return opt;
    }

    private class HandlerDelegate : ICommandHandler
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        private readonly IServiceProvider _provider;
        private readonly Type _type;
        private readonly Func<object, Delegate> _delegate;
        private readonly IRenderer _defaultConsoleRenderer;
        private readonly IRenderer[] _renderers;

        public HandlerDelegate(IServiceProvider provider, Type type, Func<object, Delegate> inner, IRenderer[] renderers, IRenderer defaultConsoleRenderer)
        {
            _renderers = renderers;
            _defaultConsoleRenderer = defaultConsoleRenderer;
            _provider = provider;
            _type = type;
            _delegate = inner;
        }

        public int Invoke(InvocationContext context)
        {
            var verb = (IVerb)_provider.GetRequiredService(_type);
            Configure(context, verb);
            var handler = CommandHandler.Create(_delegate(verb));
            return handler.Invoke(context);
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var verb = (IVerb)_provider.GetRequiredService(_type);
            Configure(context, verb);
            var handler = CommandHandler.Create(_delegate(verb));
            return handler.InvokeAsync(context);
        }

        private void Configure(InvocationContext context, IVerb verb)
        {
            if (verb is not IRenderingVerb rv) return;

            var renderer = context.BindingContext.ParseResult.RootCommandResult.Children.OfType<OptionResult>()
                .Where(o => o.Option.Name == "renderer")
                .Select(o => (IRenderer)o.GetValueOrDefault()!)
                .FirstOrDefault();

            rv.Renderer = renderer ?? _defaultConsoleRenderer;

            var noBanner = context.BindingContext.ParseResult.RootCommandResult.Children.OfType<OptionResult>()
                .Where(o => o.Option.Name == "noBanner")
                .Select(o => (bool)o.GetValueOrDefault()!)
                .FirstOrDefault();

            if (!noBanner)
                rv.Renderer.RenderBanner();
        }
    }
}
