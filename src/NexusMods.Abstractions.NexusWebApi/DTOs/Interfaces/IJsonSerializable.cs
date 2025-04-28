using System.Text.Json.Serialization.Metadata;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

/// <summary>
/// This interface marks DTOs as JSON Serializable.
/// </summary>
public interface IJsonSerializable<TSelf>
{
    /// <summary>
    /// Gets the type info needed for deserialization using source generators.
    /// </summary>
    public static abstract JsonTypeInfo<TSelf> GetTypeInfo();
}

/// <summary>
/// This interface marks DTOs as JSON Serializable for types that return an unnamed array from the API.
/// </summary>
public interface IJsonArraySerializable<TSelf>
{
    /// <summary>
    /// Gets the type info needed for deserialization using source generators.
    /// </summary>
    public static abstract JsonTypeInfo<TSelf[]> GetArrayTypeInfo();
}
