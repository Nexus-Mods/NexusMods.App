using System.Linq.Expressions;
using System.Text.Json;

#pragma warning disable CS1591

namespace NexusMods.Abstractions.Serialization.Json;

/// <summary>
/// Several classes in the app don't map directly to JSON classes. Inheritance isn't supported super well, and in some
/// cases we need to support generic types (sometimes with inheritence). Pre-generating this code is certainly possible
/// but even that sometimes results in a lot of complexity due to generic types and trying to print that data in a way
/// that the compiler will accept. This class provides a more flexible yet efficent way to map types to and from JSON data
/// </summary>
public class ConcreteConverterGenerator<T> : AExpressionConverterGenerator<T>
{
    public ConcreteConverterGenerator(IServiceProvider provider)
    {
        ReaderFunction = new Lazy<ReadDelegate>(() => GenerateReaderFunction(provider));
        WriterFunction = new Lazy<WriteDelegate>(GenerateWriter);
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
        Expression.Variable(typeof(string), "propName");
        var loopExit = Expression.Label("loopExit");

        var loopExprs = new List<Expression>();
        loopExprs.Add(readNext);
        loopExprs.Add(Expression.IfThen(Expression.Equal(Expression.Property(readerParam, "TokenType"),
            Expression.Constant(JsonTokenType.EndObject)),
            Expression.Break(loopExit)));

        var endOfSwitch = Expression.Label("endOfSwitch");

        var clauses = members.Select(m =>
        {
            if (m.IsInjected)
            {
                return null;
            }
            if (MethodMappings.TryGetValue(m.Type, out var mapping))
            {
                return Expression.SwitchCase(Expression.Block(
                        readNext,
                        Expression.Assign(vars[m.RealName], Expression.Call(readerParam, mapping.Reader, null)),
                        Expression.Break(endOfSwitch)),
                    Expression.Constant(m.RealName));
            }

            return Expression.SwitchCase(Expression.Block(
                    readNext,
                    Expression.Assign(vars[m.RealName],
                        Expression.Call(typeof(JsonSerializer), "Deserialize", new[] { m.Type }, readerParam, optionsParam)),
                    Expression.Break(endOfSwitch)),
                Expression.Constant(m.RealName));
        })
            .Where(c => c != null)
            .Select(c => c!)
            .Append(Expression.SwitchCase(
                Expression.Block(readNext, Expression.Break(endOfSwitch)),
                Expression.Constant("$type")));

        loopExprs.Add(Expression.Switch(Expression.Call(readerParam, "GetString", null),
            Expression.Throw(Expression.New(typeof(NotImplementedException).GetConstructor(new[] { typeof(string) })!,
                Expression.Add(
                    Expression.Constant($"Unknown property on {Type.Name}: "),
                    Expression.Call(readerParam, "GetString", null),
                    typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) })))),
            clauses.ToArray()));
        loopExprs.Add(Expression.Label(endOfSwitch));
        exprs.Add(Expression.Loop(Expression.Block(loopExprs)));
        exprs.Add(Expression.Label(loopExit));
        exprs.Add(Expression.MemberInit(Expression.New(Type),
            members.Select(m =>
            {
                if (m.IsInjected)
                    return Expression.Bind(m.Property, Expression.Constant(provider.GetService(m.Type)));
                return (MemberBinding)Expression.Bind(m.Property, vars[m.RealName]);
            })));

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
            if (member.IsInjected)
            {
                continue;
            }
            if (MethodMappings.TryGetValue(member.Type, out var mapping))
            {
                exprs.Add(Expression.Call(writerParam, mapping.Writer, null,
                    Expression.Constant(member.RealName), Expression.Property(valueParam, member.RealName)));
            }
            else
            {
                exprs.Add(Expression.Call(writerParam, "WritePropertyName", null, Expression.Constant(member.RealName)));
                exprs.Add(Expression.Call(typeof(JsonSerializer), "Serialize", new[] { member.Type },
                    writerParam,
                    Expression.Property(valueParam, member.RealName), optionsParam));
            }
        }

        exprs.Add(Expression.Call(writerParam, "WriteEndObject", Array.Empty<Type>()));
        var block = Expression.Block(exprs);

        return Expression.Lambda<WriteDelegate>(block, writerParam, valueParam, optionsParam).Compile();
    }
}
