using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace NexusMods.Abstractions.NexusWebApi.DTOs;

public class User
{
    [JsonPropertyName("member_id")] 
    public int MemberId { get; set; }

    [JsonPropertyName("member_group_id")] 
    public int MemberGroupId { get; set; }

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";
}


public class ModInfo : IJsonSerializable<ModInfo>
{
    /// <inheritdoc />
    public static JsonTypeInfo<ModInfo> GetTypeInfo() => ModInfoContext.Default.ModInfo;
    
    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("summary")] 
    public string Summary { get; set; } = "";

    [JsonPropertyName("description")] 
    public string Description { get; set; } = "";

    [JsonPropertyName("picture_url")] 
    public string PictureUrl { get; set; } = "";

    [JsonPropertyName("uid")] 
    public long Uid { get; set; }

    [JsonPropertyName("mod_id")] 
    public int ModId { get; set; }

    [JsonPropertyName("game_id")] 
    public int GameId { get; set; }

    [JsonPropertyName("allow_rating")] 
    public bool AllowRating { get; set; }

    [JsonPropertyName("domain_name")] 
    public string DomainName { get; set; } = "";

    [JsonPropertyName("category_id")] 
    public int CategoryId { get; set; }

    [JsonPropertyName("version")] 
    public string Version { get; set; } = "";

    [JsonPropertyName("endorsement_count")]
    public int EndorsementCount { get; set; }

    [JsonPropertyName("created_timestamp")]
    public int CreatedTimestamp { get; set; }

    [JsonPropertyName("created_time")] 
    public DateTime CreatedTime { get; set; }

    [JsonPropertyName("updated_timestamp")]
    public int UpdatedTimestamp { get; set; }

    [JsonPropertyName("updated_time")] 
    public DateTime UpdatedTime { get; set; }

    [JsonPropertyName("author")] 
    public string Author { get; set; } = "";

    [JsonPropertyName("uploaded_by")] 
    public string UploadedBy { get; set; } = "";

    [JsonPropertyName("uploaded_users_profile_url")]
    public string UploadedUsersProfileUrl { get; set; } = "";

    [JsonPropertyName("contains_adult_content")]
    public bool ContainsAdultContent { get; set; }

    [JsonPropertyName("status")] 
    public string Status { get; set; } = "";

    [JsonPropertyName("available")] 
    public bool Available { get; set; }

    [JsonPropertyName("user")] 
    public User User { get; set; } = new();
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModInfo))]
public partial class ModInfoContext : JsonSerializerContext { }
