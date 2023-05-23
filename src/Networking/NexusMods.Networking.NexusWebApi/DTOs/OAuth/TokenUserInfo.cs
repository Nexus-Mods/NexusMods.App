using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi.DTOs.OAuth;

public class TokenUserInfo
{
    [JsonPropertyName("id")]
    public ulong Id { get; init; }

    [JsonPropertyName("username")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("group_id")]
    public int GroupId { get; init; }

    [JsonPropertyName("membership_roles")]
    public MembershipRole[] MembershipRoles { get; init; } = new MembershipRole[] { };

    [JsonPropertyName("premium_expiry")]
    public ulong PremiumExpiry { get; init; }

    [JsonPropertyName("permissions")]
    public PermissionMap Permissions { get; init; } = new PermissionMap();
}
