using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs;

// Root myDeserializedClass = JsonSerializer.Deserialize<List<Root>>(myJsonResponse);
public class Category
{
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("parent_category")]
    public object ParentCategory { get; set; }
}