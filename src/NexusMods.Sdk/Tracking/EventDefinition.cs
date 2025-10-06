using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using JetBrains.Annotations;

namespace NexusMods.Sdk.Tracking;

[PublicAPI]
[DebuggerDisplay("{Name} Count = {Count}")]
public class EventDefinition : HashSet<EventPropertyDefinition>
{
    public JsonEncodedText Name { get; init; }

    public EventDefinition(string name)
    {
        Name = JsonEncodedText.Encode(name);
    }

    public EventDefinition(JsonEncodedText name)
    {
        Name = name;
    }

    public bool TryGet(string name, [NotNullWhen(true)] out EventPropertyDefinition? definition)
    {
        foreach (var currentDefinition in this)
        {
            if (!currentDefinition.Name.Value.Equals(name, StringComparison.OrdinalIgnoreCase)) continue;
            definition = currentDefinition;
            return true;
        }

        definition = null;
        return false;
    }

    public override string ToString() => $"{Name.Value}";
}

[PublicAPI]
public class EventPropertyDefinition : IEquatable<EventPropertyDefinition>
{
    public JsonEncodedText Name { get; init; }
    public Type Type { get; init; }
    public bool IsOptional { get; init; }

    private EventPropertyDefinition(JsonEncodedText name, Type type, bool isOptional)
    {
        Name = name;
        Type = type;
        IsOptional = isOptional;
    }

    public EventPropertyDefinition AsOptional()
    {
        return new EventPropertyDefinition(Name, Type, isOptional: true);
    }

    public static EventPropertyDefinition Create<T>(string name, bool isOptional = false) => new(JsonEncodedText.Encode(name), typeof(T), isOptional);
    public static EventPropertyDefinition Create<T>(JsonEncodedText name, bool isOptional = false) => new(name, typeof(T), isOptional);

    public override bool Equals(object? obj) => obj is EventPropertyDefinition other && Equals(other);
    public override int GetHashCode() => Name.GetHashCode();

    public bool Equals(EventPropertyDefinition? other) => other is not null && Name.Value.Equals(other.Name.Value, StringComparison.OrdinalIgnoreCase);

    public override string ToString() => $"{Name.Value} Type={Type} Optional={IsOptional}";
}
