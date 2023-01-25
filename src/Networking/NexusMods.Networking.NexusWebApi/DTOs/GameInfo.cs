using System.Text.Json.Serialization;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class GameInfo
{
    [JsonPropertyName("id")]
    public GameId Id { get; set; }

    [JsonPropertyName("name")] 
    public string Name { get; set; } = "";

    [JsonPropertyName("forum_url")]
    public Uri ForumUrl { get; set; }

    [JsonPropertyName("nexusmods_url")] 
    public Uri NexusmodsUrl { get; set; }

    [JsonPropertyName("genre")]
    public string Genre { get; set; } = "";

    [JsonPropertyName("file_count")]
    public ulong FileCount { get; set; }

    [JsonPropertyName("downloads")]
    public ulong Downloads { get; set; }

    [JsonPropertyName("domain_name")]
    public GameDomain DomainName { get; set; }

    [JsonPropertyName("approved_date")]
    public int ApprovedDate { get; set; }

    [JsonPropertyName("file_views")]
    public ulong? FileViews { get; set; }

    [JsonPropertyName("authors")]
    public int Authors { get; set; }

    [JsonPropertyName("file_endorsements")]
    public int FileEndorsements { get; set; }

    [JsonPropertyName("mods")]
    public ulong Mods { get; set; }

    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; }
}

