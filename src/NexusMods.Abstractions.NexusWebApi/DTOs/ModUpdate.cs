using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.V2.ModId;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Represents an individual item from a list of mods that have been updated in a
/// given period, with timestamps of their last update. Cached for 5 minutes.
/// </summary>
public class ModUpdate : IJsonArraySerializable<ModUpdate>
{
    /// <summary>
    /// For deserialization only, please use <see cref="ModId"/>.
    /// </summary>
    [JsonPropertyName("mod_id")]
    // ReSharper disable once InconsistentNaming
    public ulong _ModId { get; set; }

    /// <summary>
    /// An individual mod ID that is unique for this game.
    /// </summary>
    public ModId ModId => ModId.From((uint)_ModId);

    /// <summary>
    /// The last time a file on the mod page was updated.
    /// </summary>
    /// <remarks>
    ///    Expressed as a Unix timestamp.
    /// </remarks>
    [JsonPropertyName("latest_file_update")]
    public long LatestFileUpdated { get; set; }

    /// <summary>
    /// The last time a file on the mod page was updated.
    /// </summary>
    public DateTime LatestFileUpdatedUtc => DateTimeOffset.FromUnixTimeSeconds(LatestFileUpdated).UtcDateTime;

    /// <summary>
    /// The last time any change was made to the mod page.
    /// </summary>
    /// <remarks>
    ///    Expressed as a Unix timestamp.
    /// </remarks>
    [JsonPropertyName("latest_mod_activity")]
    public long LatestModActivity { get; set; }

    /// <summary>
    /// The last time any change was made to the mod page.
    /// </summary>
    public DateTimeOffset LatestModActivityUtc => DateTimeOffset.FromUnixTimeSeconds(LatestModActivity);

    /// <inheritdoc />
    public static JsonTypeInfo<ModUpdate[]> GetArrayTypeInfo() => ModUpdateArrayContext.Default.ModUpdateArray;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModUpdate[]))]
public partial class ModUpdateArrayContext : JsonSerializerContext { }
