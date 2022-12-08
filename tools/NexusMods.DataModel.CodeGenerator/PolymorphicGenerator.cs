using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.JsonConverters;
using NexusMods.DataModel.ModLists;

namespace NexusMods.DataModel.CodeGenerator;

public class PolymorphicGenerator<T>
{
    public PolymorphicGenerator()
    {
        var types = typeof(T).Assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .Where(t => t.IsAssignableTo(typeof(T)));

        foreach (var type in types)
        {
            var nameAttr = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonNameAttribute))
                .Select(t => (string) t.ConstructorArguments.First().Value!)
                .FirstOrDefault();

            if (nameAttr == default)
                throw new JsonException($"Type {type} of interface {typeof(T)} does not have a JsonNameAttribute");
            Registry[nameAttr] = type;
            ReverseRegistry[type] = nameAttr;

            var aliases = type.CustomAttributes.Where(t => t.AttributeType == typeof(JsonAliasAttribute))
                .Select(t => t.ConstructorArguments.First());

            foreach (var alias in aliases) Registry[(string) alias.Value!] = type;
        }
    }

    public Dictionary<string, Type> Registry { get; } = new();
    public Dictionary<Type, string> ReverseRegistry { get; } = new();

    public void GenerateSpecific(CFile c)
    {
        foreach (var type in ReverseRegistry.Keys.OrderBy(k => k.FullName))
        {
            var members = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead)
                .Where(p => p.CanWrite)
                .Where(p => !p.CustomAttributes.Any(c => c.AttributeType == typeof(JsonIgnoreAttribute)))
                .Select(p =>
                {
                    var name = p.CustomAttributes.Where(c => c.AttributeType == typeof(JsonPropertyNameAttribute))
                        .Select(a => (string) a.ConstructorArguments.FirstOrDefault().Value)
                        .FirstOrDefault() ?? p.Name;

                    return new
                    {
                        Name = name, PropName = name.ToLower() + "Prop", Property = p, Type = p.PropertyType,
                        RealName = p.Name
                    };
                })
                .OrderBy(p => p.Name)
                .ToArray();

            var mungedName = type.FullName!.Replace(".", "_");
            c.Code($"public class {mungedName}Converter : JsonConverter<{type.FullName}> {{");

            if (type.IsAssignableTo(typeof(Entity)))
            {
                c.Code("private readonly Lazy<IDataStore> _store;");
                c.Code($"public {mungedName}Converter(IServiceProvider provider) {{");
                c.Code("_store = new Lazy<IDataStore>(provider.GetRequiredService<IDataStore>);");
                c.Code("}");
            }
            
            c.Code(
                $"public override {type.FullName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {{");
            c.Code("if (reader.TokenType != JsonTokenType.StartObject)");
            c.Code("  throw new JsonException();");
            foreach (var member in members)
                GenerateMemberDeclaration(c, member.Type, member.Property, member.PropName);

            c.Code("while (true) {");

            c.Code("reader.Read();");
            c.Code("if (reader.TokenType == JsonTokenType.EndObject) {");
            c.Code("reader.Read();");
            c.Code("break;");
            c.Code("}");
            c.Code("var prop = reader.GetString();");
            c.Code("reader.Read();");
            c.Code("switch (prop) {");

            foreach (var member in members)
            {
                c.Code($"case \"{member.Name}\":");
                c.Code(
                    $"  {member.PropName} = JsonSerializer.Deserialize<{GetFriendlyTypeName(member.Type)}>(ref reader, options);");
                c.Code("  break;");
            }

            c.Code("default:");
            c.Code("  reader.Skip();");
            c.Code("  break;");

            c.Code("}");

            c.Code("}");

            c.Code($"return new {type.FullName} {{");

            foreach (var member in members) c.Code($"{member.RealName} = {member.PropName},");


            if (type.IsAssignableTo(typeof(Entity)))
                c.Code("Store = _store.Value");
            c.Code("};");

            c.Code("}");

            c.Code(
                $"public override void Write(Utf8JsonWriter writer, {type.FullName} value, JsonSerializerOptions options) {{");

            c.Code("writer.WriteStartObject();");
            c.Code($"writer.WriteString(\"$type\", \"{ReverseRegistry[type]}\");");

            foreach (var member in members)
            {
                c.Code($"writer.WritePropertyName(\"{member.Name}\");");
                c.Code(
                    $"JsonSerializer.Serialize<{GetFriendlyTypeName(member.Type)}>(writer, value.{member.RealName}, options);");
            }

            c.Code("writer.WriteEndObject();");

            c.Code("}");

            c.Code("}");
        }
    }

    private void GenerateMemberDeclaration(CFile c, Type member, PropertyInfo property, string propName)
    {
        if (member.IsGenericType)
        {
            c.Code($"{GetFriendlyTypeName(member)} {propName} = default;");
            return;
        }
        else if (member.IsGenericType && member.GetGenericTypeDefinition() == typeof(EntityHashSet<>))
        {
            c.Code($"{GetFriendlyTypeName(member)} {propName} = default;");
            return;
        }
        else
        {
            c.Code($"{GetFriendlyTypeName(member)} {propName} = default;");
        }
    }

    private string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(string))
            return "string";
        if (type == typeof(ModListId))
            return nameof(ModListId);
        if (type.IsGenericType)
        {
            // Generic names are in the format of Name`3 for Name<a, b, c>
            var baseName = type.GetGenericTypeDefinition().Name.Split("`").First();
            return
                $"{baseName}<{string.Join(",", type.GetGenericArguments().Select(GetFriendlyTypeName))}>";
        }
        if (type.IsAssignableTo(typeof(IEmptyWithDataStore<>).MakeGenericType(type)))
        {
            return type.Name;
        }
        if (type.IsAssignableTo(typeof(ICreatable<>).MakeGenericType(type)))
        {
            return type.Name;
        }
        
        if (IsGenericOf(type, typeof(EntityDictionary<,>)))
        {
            return
                $"EntityDictionary<{GetFriendlyTypeName(type.GenericTypeArguments[0])}, {GetFriendlyTypeName(type.GenericTypeArguments[1])}>";
        }

        if (IsGenericOf(type, typeof(EntityLink<>)))
        {
            return
                $"EntityLink<{GetFriendlyTypeName(type.GenericTypeArguments[0])}>";
            
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }
        else
        {
            throw new NotImplementedException();
        }
    }

    private bool IsGenericOf(Type tp, Type genericTypeDefinition)
    {
        return tp.IsGenericType && tp.GetGenericTypeDefinition() == genericTypeDefinition;
    }
    
    public void GenerateGeneric(CFile c)
    {
        var type = typeof(T);
        var mungedName = typeof(T).FullName!.Replace(".", "_");

        c.Code($"public class {mungedName}Converter : JsonConverter<{type.FullName}> {{");

        c.Code("public static void ConfigureServices(IServiceCollection services) {");

        foreach (var tp in ReverseRegistry.Keys)
            c.Code($"services.AddSingleton<JsonConverter, {tp.FullName!.Replace(".", "_")}Converter>();");
        c.Code($"services.AddSingleton<JsonConverter, {mungedName}Converter>();");

        c.Code("}");

        c.Code(
            $"public override {type.FullName} Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {{");

        c.Code("var cReader = reader;");
        c.Code("if (reader.TokenType != JsonTokenType.StartObject)");
        c.Code("  throw new JsonException();");
        c.Code("cReader.Read();");

        c.Code("if (cReader.GetString() != \"$type\")");
        c.Code("  throw new JsonException();");
        c.Code("cReader.Read();");
        c.Code("var type = cReader.GetString();");
        c.Code("switch(type) {");
        foreach (var (alias, tp) in Registry)
        {
            c.Code($"case \"{alias}\":");
            c.Code($"  return JsonSerializer.Deserialize<{GetFriendlyTypeName(tp)}>(ref reader, options)!;");
        }

        c.Code("default:");
        c.Code("  throw new JsonException($\"No Type dispatch for {type}\");");

        c.Code("}");
        c.Code("}");

        c.Code(
            $"public override void Write(Utf8JsonWriter writer, {type.FullName} value, JsonSerializerOptions options) {{");

        c.Code("switch (value) {");
        var idx = 0;

        int Distance(Type t)
        {
            var depth = 0;
            var b = t;
            while (b != null)
            {
                b = b.BaseType;
                depth += 1;
            }

            return depth;
        }

        foreach (var t in ReverseRegistry.Keys.OrderByDescending(t => Distance(t)))
        {
            c.Code($"case {t.FullName} v{idx}:");
            c.Code($"  JsonSerializer.Serialize(writer, v{idx}, options);");
            c.Code("   return;");
            idx += 1;
        }

        c.Code("}");

        c.Code("}");

        c.Code("}");
    }

    public void GenerateAll(CFile cfile)
    {
        GenerateGeneric(cfile);
        GenerateSpecific(cfile);
    }
}