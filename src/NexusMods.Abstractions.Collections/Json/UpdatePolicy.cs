using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace NexusMods.Abstractions.Collections.Json;

/// <summary>
/// Update policy.
/// </summary>
[PublicAPI]
public enum UpdatePolicy
{
    /// <summary>
    /// Use the exact version.
    /// </summary>
    [JsonStringEnumMemberName("exact")]
    ExactVersionOnly,

    /// <summary>
    /// Use the current version, if it's still available.
    /// If the file has been archived or deleted, the newest version of the file should be used.
    /// </summary>
    [JsonStringEnumMemberName("prefer")]
    PreferExact,

    /// <summary>
    /// Use the latest version.
    /// </summary>
    [JsonStringEnumMemberName("latest")]
    LatestVersion,
}
