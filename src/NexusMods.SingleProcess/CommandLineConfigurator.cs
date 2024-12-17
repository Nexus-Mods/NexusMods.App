using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.Types;
using NexusMods.ProxyConsole.Abstractions;
using NexusMods.ProxyConsole.Abstractions.VerbDefinitions;

namespace NexusMods.SingleProcess;

/// <summary>
/// A configurator for the commandline parser. It looks for all verb definitions (created by AddVerb)
/// and adds them to the parser. It also adds all injected types to the binding context. The RunAsync method
/// can be used to run the parser and execute the matching verb.
/// </summary>
public class CommandLineConfigurator
{
    private readonly RootCommand _rootCommand;
    private readonly Type[] _injectedTypes;
    private readonly IServiceProvider _provider;
    private readonly MethodInfo _makeOptionMethod;

    private ILogger _logger;

    /// <summary>
    /// DI constructor
    /// </summary>
    /// <param name="provider"></param>
    /// <param name="verbDefinitions"></param>
    public CommandLineConfigurator(IServiceProvider provider, IEnumerable<VerbDefinition> verbDefinitions)
    {
        _logger = provider.GetRequiredService<ILogger<CommandLineConfigurator>>();

        _makeOptionMethod = GetType().GetMethod(nameof(MakeOption), BindingFlags.Instance | BindingFlags.NonPublic)!;
        (_rootCommand, _injectedTypes) = MakeRootCommand(verbDefinitions);
        _provider = provider;
    }

    /// <summary>
    /// Builds the root command and returns it, along with the list of injected types.
    /// </summary>
    /// <param name="verbDefinitions"></param>
    /// <returns></returns>
    private (RootCommand, Type[]) MakeRootCommand(IEnumerable<VerbDefinition> verbDefinitions)
    {
        var rootCommand = new RootCommand();
        var injectedTypes = new HashSet<Type>();

        foreach (var verbDefinition in verbDefinitions)
        {
            var command = new Command(verbDefinition.Name, verbDefinition.Description);
            var getters = new List<Func<InvocationContext, object?>>();

            foreach (var optionDefinition in verbDefinition.Options)
            {
                if (optionDefinition.IsInjected)
                {
                    // Injected options are pulled from the service provider via the binding context
                    injectedTypes.Add(optionDefinition.Type);
                    getters.Add(ctx => ctx.BindingContext.GetRequiredService(optionDefinition.Type));
                }
                else
                {
                    var option = (Option)_makeOptionMethod.MakeGenericMethod(optionDefinition.Type)
                        .Invoke(this, new[] { optionDefinition })!;
                    command.AddOption(option);
                    getters.Add(ctx => ctx.ParseResult.GetValueForOption(option));
                }
            }
            command.Handler = new CommandHandler(getters, verbDefinition.Info);

            rootCommand.AddCommand(command);
        }
        return (rootCommand, injectedTypes.ToArray());
    }

    private Option MakeOption<T>(OptionDefinition optionDefinition)
    {
        var aliases = new[] { "-" + optionDefinition.ShortName, "--" + optionDefinition.LongName };

        ParseArgument<T> parser = result =>
        {
            var service = _provider.GetService<IOptionParser<T>>();
            if (service == null)
                return default!;
            if (service.TryParse(result.Tokens.Single().Value, out var itm, out var error))
                return itm;
            result.ErrorMessage = error;
            return default!;

        };

        var option = new Option<T>(aliases, parser, false, optionDefinition.HelpText);
        return option;
    }

    private async Task<bool> RunLink(string[] args, CancellationToken cancellationToken)
    {
        // NOTE(erri120): The CLI framework we're using was designed for verbs and options only.
        // This method circumvents the entire framework to deal with this invocation:
        // NexusMods.App nxm://something
        // This invocation can't be parsed with the CLI framework, so we hand-role the parsing.
        // See https://github.com/Nexus-Mods/NexusMods.App/issues/1677 for details.

        if (args.Length != 1) return false;
        if (!Uri.TryCreate(args[0], UriKind.Absolute, out var uri)) return false;

        var handlers = _provider.GetServices<IIpcProtocolHandler>().ToArray();
        var handler = handlers.FirstOrDefault(handler => handler.Protocol.Equals(uri.Scheme, StringComparison.OrdinalIgnoreCase));

        if (handler is null) return false;

        try
        {
            _logger.LogInformation("Using handler {Handler} for `{Scheme}`", handler.GetType(), uri.Scheme);
            await handler.Handle(uri.ToString(), cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while running handler {Handler} for `{Uri}`", handler.GetType(), uri);
        }

        return true;
    }

    /// <summary>
    /// Runs the commandline parser and executes the verb using the given renderer and arguments.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="renderer"></param>
    /// <param name="token"></param>
    /// <returns></returns>
    public async Task<int> RunAsync(string[] args, IRenderer renderer, CancellationToken token)
    {
        if (await RunLink(args, token)) return 0;
        if (_logger.IsEnabled(LogLevel.Debug)) _logger.LogDebug("Received command `{Command}`", string.Join(" ", args));

        var parser = new CommandLineBuilder(_rootCommand)
            .UseHelp()
            .UseParseErrorReporting()
            .AddMiddleware((ctx, next) =>
            {
                foreach (var type in _injectedTypes)
                {
                    if (type == typeof(IRenderer))
                        ctx.BindingContext.AddService(type, _ => renderer);
                    else if (type == typeof(CancellationToken))
                        ctx.BindingContext.AddService(type, _ => token);
                    else
                        ctx.BindingContext.AddService(type, _ => _provider.GetRequiredService(type));
                }
                return next(ctx);
            })
            .Build();

        return await parser.InvokeAsync(args, new ConsoleToRendererAdapter(renderer));
    }
}
