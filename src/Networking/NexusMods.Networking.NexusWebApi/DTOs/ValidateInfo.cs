using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class ValidateInfo
{
    [JsonPropertyName("user_id")] 
    public int UserId { get; set; }

    [JsonPropertyName("key")] 
    public string Key { get; set; } = "";

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("is_premium?")] 
    public bool _IsPremium { get; set; }

    [JsonPropertyName("is_supporter?")] 
    public bool _IsSupporter { get; set; }

    [JsonPropertyName("email")] 
    public string Email { get; set; } = "";

    [JsonPropertyName("profile_url")] 
    public Uri? ProfileUrl { get; set; }

    [JsonPropertyName("is_supporter")] 
    public bool IsSupporter { get; set; }

    [JsonPropertyName("is_premium")] 
    public bool IsPremium { get; set; }
}