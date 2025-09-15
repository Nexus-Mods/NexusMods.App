using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Collections.Json;

public class Option
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("groups")]
    public Group[] groups { get; init; } = [];
}

public class Group
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public Choice[] choices { get; init; } = [];
}

public class Choice
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("idx")]
    public int idx { get; init; }
}
