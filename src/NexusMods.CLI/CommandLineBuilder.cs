using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Interfaces.Components;
using NexusMods.Paths;

namespace NexusMods.CLI;

public class CommandLineBuilder
{
    private static IServiceProvider _provider = null!;
    public CommandLineBuilder(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public async Task<int> Run(string[] args)
    {
        var root = new RootCommand();
        foreach (var verb in _commands)
        {
            root.Add(MakeCommend(verb.Type, verb.Handler, verb.Definition));
        }

        return await root.InvokeAsync(args);
    }

    private Dictionary<Type, Func<OptionDefinition, Option>> _optionCtors => new()
    {
        {
            typeof(string),
            d => new Option<string>(d.Aliases, description: d.Description)
        },
        {
            typeof(AbsolutePath),
            d => new Option<AbsolutePath>(d.Aliases, description: d.Description, parseArgument: d => d.Tokens.Single().Value.ToAbsolutePath())
        },
        {
            typeof(Uri),
            d => new Option<Uri>(d.Aliases, description: d.Description)
        },
        {
            typeof(bool),
            d => new Option<bool>(d.Aliases, description: d.Description)
        },
        {
            typeof(IGame),
            d => new Option<IGame>(d.Aliases, description: d.Description, parseArgument: d =>
            {
                var s = d.Tokens.Single().Value;
                var games = _provider.GetRequiredService<IEnumerable<IGame>>();
                return games.First(d => d.Slug.Equals(s, StringComparison.InvariantCultureIgnoreCase));
            })
        },
        {
            typeof(Version),
            d => new Option<Version>(d.Aliases, description: d.Description, parseArgument: d => Version.Parse(d.Tokens.Single().Value))
        }
    };


    private Command MakeCommend(Type verbType, Func<object, Delegate> verbHandler, VerbDefinition definition)
    {
        var command = new Command(definition.Name, definition.Description);
        foreach (var option in definition.Options)
        {
            command.Add(_optionCtors[option.Type](option));
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
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delgate(service));
            return handler.Invoke(context);
        }

        public Task<int> InvokeAsync(InvocationContext context)
        {
            var service = _provider.GetRequiredService(_type);
            var handler = CommandHandler.Create(_delgate(service));
            return handler.InvokeAsync(context);
        }
    }

    private static List<(Type Type, VerbDefinition Definition, Func<object, Delegate> Handler)> _commands { get; set; } = new();
    public static IEnumerable<Type> Verbs => _commands.Select(c => c.Type);

    public static void RegisterCommand<T>(VerbDefinition definition, Func<object, Delegate> handler)
    {
        _commands.Add((typeof(T), definition, handler));
        
    }
}

public record OptionDefinition(Type Type, string ShortOption, string LongOption, string Description)
{
    public string[] Aliases
    {
        get
        {
            return new[] { "-" + ShortOption, "--" + LongOption };
        }
    } 
}

public record VerbDefinition(string Name, string Description, OptionDefinition[] Options)
{
}

