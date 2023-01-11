using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;

namespace NexusMods.DataModel.JsonConverters;

public class AbstractClassConverterGenerator<T> : AExpressionConverterGenerator<T>
{
    public AbstractClassConverterGenerator(IServiceProvider provider) : base(provider)
    {
        _readerFunction = new Lazy<ReadDelegate>(() => GenerateReaderFunction(Provider));
        _writerFunction = new Lazy<WriteDelegate>(GenerateWriter);
    }

    private WriteDelegate GenerateWriter()
    {
        var registry = new Dictionary<string, Type>();
        var reverseRegistry = new Dictionary<Type, string>();

        foreach (var type in GetSubClasses())
        {
            var nameAttr = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonNameAttribute))
                .Select(t => (string) t.ConstructorArguments.First().Value!)
                .FirstOrDefault();

            if (nameAttr == default)
                throw new JsonException($"Type {type} of interface {Type} does not have a JsonNameAttribute");
            registry[nameAttr] = type;
            reverseRegistry[type] = nameAttr;

            var aliases = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonAliasAttribute))
                .Select(t => t.ConstructorArguments.First());

            foreach (var alias in aliases) 
                registry[(string) alias.Value!] = type;
        }
        
        var writerParam = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
        var valueParam = Expression.Parameter(typeof(T), "value");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

        var exprs = new List<Expression>();

        var switches = new List<SwitchCase>();
        
        var endOfSwitch = Expression.Label("endOfSwitch");

        foreach (var (name, type) in registry)
        {
            switches.Add(Expression.SwitchCase(
                Expression.Block(Expression.Call(typeof(JsonSerializer), "Serialize", new []{type},
                writerParam, Expression.TypeAs(valueParam, type), optionsParam),
                    Expression.Break(endOfSwitch)),
            Expression.Constant(type)));
        }

        
        exprs.Add(Expression.Switch(Expression.Call(Expression.TypeAs(valueParam, typeof(object)), "GetType", null), 
            Expression.Throw(Expression.New(typeof(JsonException).GetConstructor(new[] { typeof(string) })!,
                Expression.Constant("Unregistered subclass in serialization"))),
            switches.ToArray()));
        exprs.Add(Expression.Label(endOfSwitch));
        var block = Expression.Block(exprs);

        var lambda = Expression.Lambda<WriteDelegate>(block, writerParam, valueParam, optionsParam);
        return lambda.Compile();
    }

    private ReadDelegate GenerateReaderFunction(IServiceProvider provider)
    {
        throw new NotImplementedException();
    }
    
    private IEnumerable<Type> GetSubClasses()
    {
        var finders = Provider.GetRequiredService<IEnumerable<ITypeFinder>>();
        return finders.SelectMany(f => f.DescendentsOf(Type))
            .Where(d => !d.IsAbstract && !d.IsInterface)
            .Distinct();
    }
}