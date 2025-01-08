// ReSharper disable InconsistentNaming

using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.Collections.Json;

/// <summary>
/// The possible sources of a mod
/// </summary>
public enum ModSourceType
{
    /// <summary>
    /// Sourced from Nexus Mods.
    /// </summary>
    [JsonStringEnumMemberName("nexus")]
    NexusMods,

    /// <summary>
    /// Bundled with the collection archive.
    /// </summary>
    [JsonStringEnumMemberName("bundle")]
    Bundle,

    /// <summary>
    /// Downloaded externally via an URL.
    /// </summary>
    [JsonStringEnumMemberName("browse")]
    Browse,

    /// <summary>
    /// Downloaded externally via an URL.
    /// </summary>
    [JsonStringEnumMemberName("direct")]
    Direct,
}
