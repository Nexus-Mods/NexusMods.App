using System.Text.Json.Serialization;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Networking.NexusWebApi.DTOs;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618 

/// <summary>
/// Mod categories. Unique per game.
/// i.e. Each game has its own set of categories.
/// </summary>
public class Category
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

    // TODO: Convenient way to handle this.
    
    /// <summary>
    /// Category which owns this category.
    /// </summary>
    /// <remarks>
    ///    This field can either be represented as a <see cref="CategoryId"/> of another category or
    ///    'false' if there is no parent. 
    /// </remarks>
    [JsonPropertyName("parent_category")]
    public object ParentCategory { get; set; }
}