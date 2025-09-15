using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Collections.Json;

public class Choices
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public required ChoicesType Type { get; init; }

    [JsonPropertyName("options")]
    public Option[] Options { get; init; } = [];
}
