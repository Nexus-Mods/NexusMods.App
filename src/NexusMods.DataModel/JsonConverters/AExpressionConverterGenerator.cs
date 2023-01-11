using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.DataModel.JsonConverters;

public abstract class AExpressionConverterGenerator<T> : JsonConverter<T>
{
    protected readonly IServiceProvider Provider;
    protected Lazy<WriteDelegate> _writerFunction;
    protected Lazy<ReadDelegate> _readerFunction;
    protected readonly Type Type;

    protected delegate T? ReadDelegate(ref Utf8JsonReader read, Type typeToConvert, JsonSerializerOptions options);

    protected delegate void WriteDelegate(Utf8JsonWriter writer, T value, JsonSerializerOptions options);

    public static readonly Dictionary<Type, (string Writer, string Reader)> MethodMappings = new()
    {
        { typeof(int), ("WriteNumber", "GetInt32") },
        { typeof(uint), ("WriteNumber", "GetUInt32") },
        { typeof(long), ("WriteNumber", "GetInt64") },
        { typeof(ulong), ("WriteNumber", "GetUInt64") },
        { typeof(string), ("WriteString", "GetString")},
    };

    protected AExpressionConverterGenerator(IServiceProvider provider)
    {
        Provider = provider;
        Type = typeof(T);
    }
    
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return _readerFunction.Value(ref reader, typeToConvert, options);
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        _writerFunction.Value(writer, value, options);
    }
    
    protected static MemberRecord[] GetMembers()
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

    protected string GetNameAttr()
    {
        var nameAttr = Type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonNameAttribute))
            .Select(t => (string)t.ConstructorArguments.First().Value!)
            .FirstOrDefault();

        if (nameAttr == default)
            throw new JsonException($"Type {Type} does not have a JsonNameAttribute");
        return nameAttr;
    }
    
    protected class MemberRecord
    {
        public required string Name { get; init; }
        public required string PropName { get; init; }
        public required PropertyInfo Property { get; init; }
        public required Type Type { get; init; }
        public required string RealName { get; init; }
    }

}