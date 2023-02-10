using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.CLI.OptionParsers;

namespace NexusMods.CLI;

/// <summary>
/// Generates the command line parsing structures, and connects them to verbs
/// </summary>
public class CommandLineConfigurator
{
    private static IServiceProvider _provider = null!;
    private readonly ILogger<CommandLineConfigurator> _logger;
    private readonly IEnumerable<Verb> _verbs;

    public CommandLineConfigurator(ILogger<CommandLineConfigurator> logger, IEnumerable<Verb> verbs, IServiceProvider provider)
    {
        _logger = logger;
        _provider = provider;
        _verbs = verbs.ToArray();
    }
    
    public RootCommand MakeRoot()
    {
        var root = new RootCommand();
        var renderOption = new Option<IRenderer>("--renderer", parseArgument:
            x =>
            {
                var found = _provider.GetServices<IRenderer>()
                    .FirstOrDefault(r => r.Name == x.Tokens.Single().Value);
                if (found == null)
                    throw new Exception($"Invalid renderer {x.Tokens.Single()}");
                return found;
            });
        root.AddOption(renderOption);
        
        root.AddOption(new Option<bool>("--noBanner"));
        
        foreach (var verb in _verbs)
        {
            root.Add(MakeCommend(verb.Type, verb.Run, verb.Definition));
        }

        return root;
    }

    private Command MakeCommend(Type verbType, Func<object, Delegate> verbHandler, VerbDefinition definition)
    {
        var command = new Command(definition.Name, definition.Description);

        foreach (var option in definition.Options)
        {
            command.Add(option.GetOption(_provider));
        }
        command.Handler = new HandlerDelegate(_provider, verbType, verbHandler);
        return command;
    }
    
    private class HandlerDelegate : ICommandHandler
    {
        private IServiceProvider _provider;
        private Type _type;
        private readonly Func<object, Delegate> _delgate;

        public HandlerDelegate(IServiceProvider provider, Type type, Func<object, Delegate> inner)
        {
            _provider = provider;
            _type = type;
            _delgate = inner;
        }
        public int Invoke(InvocationContext context)
        {
            var configurator = _provider.GetRequiredService<Configurator>();
            configurator.Configure(context);
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delgate(service));
            return handler.Invoke(context);
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var configurator = _provider.GetRequiredService<Configurator>();
            configurator.Configure(context);
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delgate(service));
            return handler.InvokeAsync(context);
        }
    }
}

public record OptionDefinition<T>(string ShortOption, string LongOption, string Description) 
    : OptionDefinition(typeof(T), ShortOption, LongOption, Description)
{
    public override Option GetOption(IServiceProvider provider)
    {
        var converter = provider.GetService<IOptionParser<T>>();
        
        if (converter == null)
        {
            return new Option<T>(Aliases, description: Description);
        }

        var opt = new Option<T>(Aliases, description: Description,
            parseArgument: x => converter.Parse(x.Tokens.Single().Value, this));
        opt.AddCompletions(x => converter.GetOptions(x.WordToComplete));
        return opt;
    }
}
public abstract record OptionDefinition(Type Type, string ShortOption, string LongOption, string Description)
{
    public string[] Aliases
    {
        get
        {
            return new[] { "-" + ShortOption, "--" + LongOption };
        }
    }

    public abstract Option GetOption(IServiceProvider provider);
}

public record VerbDefinition(string Name, string Description, OptionDefinition[] Options)
{
}

