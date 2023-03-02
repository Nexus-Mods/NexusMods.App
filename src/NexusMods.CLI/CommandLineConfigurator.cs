using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI.OptionParsers;

namespace NexusMods.CLI;

/// <summary>
/// Generates the command line parsing structures, and connects them to verbs
/// </summary>
public class CommandLineConfigurator
{
    private static IServiceProvider _provider = null!;
    private readonly IEnumerable<Verb> _verbs;

    /// <summary/>
    /// <param name="verbs">
    ///     List of supported verbs.  
    ///     This is populated by DI; multiple registrations using to <see cref="IServiceCollection"/> is resolved as
    ///     an enumerable of verbs.  
    /// </param>
    /// <param name="provider">Instance of dependency injection container.</param>
    public CommandLineConfigurator(IEnumerable<Verb> verbs, IServiceProvider provider)
    {
        _provider = provider;
        _verbs = verbs.ToArray();
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
            command.Add(option.GetOption(_provider));

        command.Handler = new HandlerDelegate(_provider, verbType, verbHandler);
        return command;
    }

    private class HandlerDelegate : ICommandHandler
    {
        // ReSharper disable once MemberHidesStaticFromOuterClass
        private readonly IServiceProvider _provider;
        private readonly Type _type;
        private readonly Func<object, Delegate> _delegate;

        public HandlerDelegate(IServiceProvider provider, Type type, Func<object, Delegate> inner)
        {
            _provider = provider;
            _type = type;
            _delegate = inner;
        }

        public int Invoke(InvocationContext context)
        {
            var configurator = _provider.GetRequiredService<Configurator>();
            configurator.Configure(context);
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delegate(service));
            return handler.Invoke(context);
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var configurator = _provider.GetRequiredService<Configurator>();
            configurator.Configure(context);
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delegate(service));
            return handler.InvokeAsync(context);
        }
    }
}

public record OptionDefinition<T>(string ShortOption, string LongOption, string Description) : OptionDefinition(ShortOption, LongOption, Description)
{
    public override Option GetOption(IServiceProvider provider)
    {
        var converter = provider.GetService<IOptionParser<T>>();

        if (converter == null)
            return new Option<T>(Aliases, description: Description);

        var opt = new Option<T>(Aliases, description: Description,
            parseArgument: x => converter.Parse(x.Tokens.Single().Value, this));

        opt.AddCompletions(x => converter.GetOptions(x.WordToComplete));
        return opt;
    }
}
public abstract record OptionDefinition(string ShortOption, string LongOption, string Description)
{
    protected string[] Aliases => new[] { "-" + ShortOption, "--" + LongOption };

    public abstract Option GetOption(IServiceProvider provider);
}
