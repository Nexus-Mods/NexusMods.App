using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;

/// <summary>
/// Represents user information from an OAuth JWT token provided by the Nexus Mods API.
/// </summary>
public class TokenUserInfo
{
    /// <summary>
    /// Gets or sets the user's id.
    /// </summary>
    [JsonPropertyName("id")]
    public ulong Id { get; init; }

    /// <summary>
    /// Gets or sets the user's name.
    /// </summary>
    [JsonPropertyName("username")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets the user's group id.
    /// </summary>
    [JsonPropertyName("group_id")]
    public int GroupId { get; init; }

    /// <summary>
    /// Gets or sets the user's membership roles. (e.g. Premium)
    /// </summary>
    [JsonPropertyName("membership_roles")]
    public MembershipRole[] MembershipRoles { get; init; } = Array.Empty<MembershipRole>();

    /// <summary>
    /// Gets or sets the user's premium expiry time as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("premium_expiry")]
    public ulong PremiumExpiry { get; init; }

    /// <summary>
    /// Gets or sets the OAuth Token's permission mapping.
    /// </summary>
    [JsonPropertyName("permissions")]
    public PermissionMap Permissions { get; init; } = new();
}
