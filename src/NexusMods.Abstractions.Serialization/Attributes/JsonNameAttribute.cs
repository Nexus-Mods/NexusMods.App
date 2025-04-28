using System.Text.Json.Serialization;
using NexusMods.Abstractions.Serialization.Json;

namespace NexusMods.Abstractions.Serialization.Attributes;

/// <summary>
/// Marks a class for <see cref="JsonConverter{T}"/> generation by <see cref="AExpressionConverterGenerator{T}"/> (and friends).
/// Types marked with this attribute participate in the polymorphic deserialization
/// features of the project and their data is marked by the $type: "name" field on each
/// JSON object
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class JsonNameAttribute : Attribute
{
    /// <summary>
    /// Name of the item as serialized in JSON.
    /// </summary>
    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public string Name { get; }

    /// <inheritdoc />
    public JsonNameAttribute(string name) => Name = name;
}
