using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;
using NexusMods.Sdk.NexusModsApi;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Represents information about an individual game.
/// </summary>
/// <remarks>
///    Usually returned in batch from a single API call.
/// </remarks>
public class GameInfo : IJsonArraySerializable<GameInfo>
{
    /// <summary>
    /// Unique identifier for the game.
    /// </summary>
    /// <remarks>
    ///    Consider using <see cref="Id"/> instead.
    ///    This field is for deserialization only.
    /// </remarks>
    [JsonPropertyName("id")]
    public uint _Id { get; set; }

    /// <summary>
    /// Returns the ID as typed ValueObject <see cref="GameId"/>.
    /// </summary>
    public GameId Id => GameId.From(_Id);

    /// <summary>
    /// Name of the game.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// URL to the forum section on the Nexus forums dedicated to this game.
    /// </summary>
    [JsonPropertyName("forum_url")]
    public Uri ForumUrl { get; set; }

    /// <summary>
    /// URL to the Nexus page for this game.
    /// </summary>
    [JsonPropertyName("nexusmods_url")]
    public Uri NexusModsUrl { get; set; }

    /// <summary>
    /// Game's main genre.
    /// </summary>
    [JsonPropertyName("genre")]
    public string Genre { get; set; }

    /// <summary>
    /// Total number of uploaded files for this game title.
    /// </summary>
    [JsonPropertyName("file_count")]
    public ulong FileCount { get; set; }

    /// <summary>
    /// Number of total combined downloads across all mods for this title.
    /// </summary>
    [JsonPropertyName("downloads")]
    public ulong Downloads { get; set; }

    /// <summary>
    /// Represents the string id/name of the game on the Nexus site.
    /// e.g. 'morrowind' for 'https://nexusmods.com/morrowind'
    /// </summary>
    /// <remarks>
    ///    This value is often used in API requests.
    /// </remarks>
    [JsonPropertyName("domain_name")]
    public string DomainName { get; set; }

    /// <summary>
    /// Unix timestamp of when the game was approved by the site staff.
    /// </summary>
    [JsonPropertyName("approved_date")]
    public int ApprovedDate { get; set; }

    /// <summary>
    /// Timestamp of when the game was approved by the site staff, expressed as UTC, Coordinated Universal Time.
    /// </summary>
    public DateTime ApprovedDateUtc => DateTimeOffset.FromUnixTimeSeconds(ApprovedDate).UtcDateTime;

    /// <summary>
    /// Number of views on this file.
    /// </summary>
    [JsonPropertyName("file_views")]
    public ulong? FileViews { get; set; }

    /// <summary>
    /// Number of individual mod authors.
    /// </summary>
    [JsonPropertyName("authors")]
    public int Authors { get; set; }

    /// <summary>
    /// Number of endorsements received on individual files on Nexus
    /// </summary>
    [JsonPropertyName("file_endorsements")]
    public int FileEndorsements { get; set; }

    /// <summary>
    /// Total number of mods stored for this game.
    /// </summary>
    [JsonPropertyName("mods")]
    public ulong Mods { get; set; }

    /// <summary>
    /// List of available categories for this individual game.
    /// </summary>
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<GameInfo[]> GetArrayTypeInfo() => GameInfoArrayContext.Default.GameInfoArray;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(GameInfo[]))]
public partial class GameInfoArrayContext : JsonSerializerContext { }
