using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Several classes in the app don't map directly to JSON classes. Inheritence isn't supported super well, and in some
/// cases we need to support generic types (sometimes with inheritence). Pre-generating this code is certainly possible
/// but even that sometimes results in a lot of complexity due to generic types and trying to print that data in a way
/// that the compiler will accept. This class provides a more flexible yet efficent way to map types to and from JSON data
/// </summary>
public class ExpressionGeneratorConverter<T> : JsonConverter<T>
{
    private readonly IServiceProvider _provider;
    private readonly Lazy<WriteDelegate> _writerFunction;
    private readonly Lazy<ReadDelegate> _readerFunction;
    private readonly Type _type;

    private delegate T? ReadDelegate(ref Utf8JsonReader read, Type typeToConvert, JsonSerializerOptions options);
    private delegate void WriteDelegate(Utf8JsonWriter writer, T value, JsonSerializerOptions options);


    private static Dictionary<Type, (string Writer, string Reader)> _methodMappings = new()
    {
        { typeof(int), ("WriteNumber", "GetInt32") },
        { typeof(uint), ("WriteNumber", "GetUInt32") },
        { typeof(long), ("WriteNumber", "GetInt64") },
        { typeof(ulong), ("WriteNumber", "GetUInt64") },
        { typeof(string), ("WriteString", "GetString")},
    };

    public ExpressionGeneratorConverter(IServiceProvider provider)
    {
        _type = typeof(T);
        _provider = provider;
        _readerFunction = new Lazy<ReadDelegate>(() => GenerateReaderFunction(provider));
        _writerFunction = new Lazy<WriteDelegate>(GenerateWriter);
    }

    private ReadDelegate GenerateReaderFunction(IServiceProvider provider)
    {
        var members = GetMembers();

        var readerParam = Expression.Parameter(typeof(Utf8JsonReader).MakeByRefType(), "reader");
        var typeToConvertParam = Expression.Parameter(typeof(Type), "typeToConvert");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");

        var exprs = new List<Expression>();

        exprs.Add(Expression.IfThen(Expression.NotEqual(Expression.Property(readerParam, "TokenType"),
            Expression.Constant(JsonTokenType.StartObject)),
            Expression.Throw(Expression.New(typeof(JsonException).GetConstructor(new[] { typeof(string) })!,
                Expression.Constant("Expected StartObject")))));

        var readNext = Expression.Call(readerParam, "Read", null);

        var vars = members.ToDictionary(m => m.RealName, m => Expression.Variable(m.Type, m.PropName));
        
        var propName = Expression.Variable(typeof(string), "propName");
        
        var loopExit = Expression.Label("loopExit");
        
        var loopExprs = new List<Expression>();
        loopExprs.Add(readNext);
        loopExprs.Add(Expression.IfThen(Expression.Equal(Expression.Property(readerParam, "TokenType"),
            Expression.Constant(JsonTokenType.EndObject)),
            Expression.Break(loopExit)));
        
        var endOfSwitch = Expression.Label("endOfSwitch");

        var clauses = members.Select(m =>
        {
            if (_methodMappings.TryGetValue(m.Type, out var mapping))
            {
                return Expression.SwitchCase(Expression.Block(
                    readNext,
                    Expression.Assign(vars[m.RealName], Expression.Call(readerParam, mapping.Reader, null)),
                    Expression.Break(endOfSwitch)),
                    Expression.Constant(m.RealName));
            }
            else
            {
                return Expression.SwitchCase(Expression.Block(
                    readNext,
                    Expression.Assign(vars[m.RealName],
                        Expression.Call(typeof(JsonSerializer), "Deserialize", new [] {m.Type}, readerParam, optionsParam)),
                    Expression.Break(endOfSwitch)),
                    Expression.Constant(m.RealName));
            }
        });
        
        loopExprs.Add(Expression.Switch(Expression.Call(readerParam, "GetString", null), null, clauses.ToArray()));
        loopExprs.Add(Expression.Label(endOfSwitch));
        exprs.Add(Expression.Loop(Expression.Block(loopExprs)));
        exprs.Add(Expression.Label(loopExit));
        exprs.Add(Expression.MemberInit(Expression.New(_type), 
            members.Select(m => (MemberBinding)Expression.Bind(m.Property, vars[m.RealName]))));

        var block = Expression.Block(vars.Values, exprs);
        
        var d = Expression.Lambda<ReadDelegate>(block, false, readerParam, typeToConvertParam, optionsParam);
        return d.Compile();

    }

    private WriteDelegate GenerateWriter()
    {
        var nameAttr = GetNameAttr();
        var members = GetMembers();
        
        var writerParam = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
        var valueParam = Expression.Parameter(typeof(T), "value");
        var optionsParam = Expression.Parameter(typeof(JsonSerializerOptions), "options");


        List<Expression> exprs = new();
        
        exprs.Add(Expression.Call(writerParam, "WriteStartObject", null));
        exprs.Add(Expression.Call(writerParam, "WriteString", null, 
            Expression.Constant("$type"), Expression.Constant(nameAttr)));

        foreach (var member in members)
        {
            if (_methodMappings.TryGetValue(member.Type, out var mapping))
            {
                exprs.Add(Expression.Call(writerParam, mapping.Writer, null,
                    Expression.Constant(member.RealName), Expression.Property(valueParam, member.RealName)));
            }
            else
            {
                exprs.Add(Expression.Call(writerParam, "WritePropertyName", null, Expression.Constant(member.RealName)));
                exprs.Add(Expression.Call(typeof(JsonSerializer), "Serialize", new []{member.Type}, 
                    writerParam, 
                    Expression.Property(valueParam, member.RealName), optionsParam));
            }
        }
        
        exprs.Add(Expression.Call(writerParam, "WriteEndObject", Array.Empty<Type>()));
            
            

        var block = Expression.Block(exprs);

        return Expression.Lambda<WriteDelegate>(block, writerParam, valueParam, optionsParam).Compile();
    }

    private static MemberRecord[] GetMembers()
    {
        var members = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .Where(p => p.CanWrite)
            .Where(p => p.CustomAttributes.All(c => c.AttributeType != typeof(JsonIgnoreAttribute)))
            .Select(p =>
            {
                var name = p.CustomAttributes.Where(c => c.AttributeType == typeof(JsonPropertyNameAttribute))
                    .Select(a => (string)a.ConstructorArguments.FirstOrDefault().Value!)
                    .FirstOrDefault() ?? p.Name;

                return new MemberRecord
                {
                    Name = name,
                    PropName = name.ToLower() + "Prop",
                    Property = p,
                    Type = p.PropertyType,
                    RealName = p.Name
                };
            })
            .OrderBy(p => p.Name)
            .ToArray();
        return members;
    }

    private string GetNameAttr()
    {
        var nameAttr = _type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonNameAttribute))
            .Select(t => (string)t.ConstructorArguments.First().Value!)
            .FirstOrDefault();

        if (nameAttr == default)
            throw new JsonException($"Type {_type} does not have a JsonNameAttribute");
        return nameAttr;
    }

    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return _readerFunction.Value(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        _writerFunction.Value(writer, value, options);
    }

    private class MemberRecord
    {
        public required string Name { get; init; }
        public required string PropName { get; init; }
        public required PropertyInfo Property { get; init; }
        public required Type Type { get; init; }
        public required string RealName { get; init; }
    }
}