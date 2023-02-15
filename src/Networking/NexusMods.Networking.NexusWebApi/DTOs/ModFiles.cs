using System.Text.Json.Serialization;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618 

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Networking.NexusWebApi.DTOs;

/// <summary>
/// Represents the collection of downloadable files tied to a mod.
/// </summary>
public class ModFiles
{
    /// <summary/>
    [JsonPropertyName("files")]
    public ModFile[] Files { get; set; }
}
