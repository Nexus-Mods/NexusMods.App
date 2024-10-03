using System.Text.Json.Serialization;

namespace NexusMods.Games.FOMOD;

public class FomodOption
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("groups")]
    public FomodGroup[] groups { get; init; } = [];
}

public class FomodGroup
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("choices")]
    public FomodChoice[] choices { get; init; } = [];
}

public class FomodChoice
{
    [JsonPropertyName("name")]
    public string name { get; init; } = string.Empty;
    
    [JsonPropertyName("idx")]
    public int idx { get; init; }
}
