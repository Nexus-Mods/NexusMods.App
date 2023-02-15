using System.Text.Json.Serialization;
using NexusMods.Networking.NexusWebApi.Types;


// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618 

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Networking.NexusWebApi.DTOs;

/// <summary>
/// Represents an individual item from a list of mods that have been updated in a
/// given period, with timestamps of their last update. Cached for 5 minutes.
/// </summary>
public class ModUpdate
{
    /// <summary>
    /// An individual mod ID that is unique for this game.
    /// </summary>
    [JsonPropertyName("mod_id")]
    public ModId ModId { get; set; }

    // TODO: This is only referenced from test harness (right now). I think this might be incorrectly defined; since API returns timestamps; which shouldn't deserialize here.

    /// <summary>
    /// The last time a file on the mod page was updated.
    /// </summary>
    /// <remarks>
    ///    Expressed as a Unix timestamp.
    /// </remarks>
    [JsonPropertyName("LatestFileUpdated")]
    public DateTime LatestFileUpdated { get; set; }
    
    /// <summary>
    /// The last time any change was made to the mod page.
    /// </summary>
    /// <remarks>
    ///    Expressed as a Unix timestamp.
    /// </remarks>
    [JsonPropertyName("LatestModActivity")]
    public DateTime LatestModActivity { get; set; }
}