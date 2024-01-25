using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618

/// <summary>
/// Mod categories. Unique per game.
/// i.e. Each game has its own set of categories.
/// </summary>
public class Category : IJsonSerializable<Category>
{
    /// <summary>
    /// Unique identifier for the category.
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Human readable of the category.
    /// This gets displayed to the end user.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    // TODO: Convenient way to handle this. [Note: Whether we will pick on this up is unknown; depends on state of GraphQL API down the road]

    /// <summary>
    /// Category which owns this category.
    /// </summary>
    /// <remarks>
    ///    This field can either be represented as a <see cref="CategoryId"/> of another category or
    ///    'false' if there is no parent.
    /// </remarks>
    [JsonPropertyName("parent_category")]
    public object ParentCategory { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<Category> GetTypeInfo() => CategoryContext.Default.Category;
}

// Note for future readers: JsonSourceGenerationMode.Serialization is for Serialization only;
// this code will be redundant for us as we deserialize only; hence we don't generate it.
/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(Category))]
public partial class CategoryContext : JsonSerializerContext { }
