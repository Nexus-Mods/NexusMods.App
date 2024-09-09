using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Collections.Json;

public class Choices
{
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
