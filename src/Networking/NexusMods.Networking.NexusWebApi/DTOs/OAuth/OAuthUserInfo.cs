using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JetBrains.Annotations;
using NexusMods.Networking.NexusWebApi.DTOs.Interfaces;

namespace NexusMods.Networking.NexusWebApi.DTOs.OAuth;

/// <summary>
/// Data returned by the OAuth userinfo endpoint.
/// </summary>
[PublicAPI]
public record OAuthUserInfo : IJsonSerializable<OAuthUserInfo>
{
    /// <summary>
    /// Gets the User ID.
    /// </summary>
    [JsonPropertyName("sub")]
    public ulong Sub { get; init; }

    /// <summary>
    /// Gets the User Name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the avatar url.
    /// </summary>
    [JsonPropertyName("avatar")]
    public Uri? Avatar { get; init; }

    /// <summary>
    /// Gets an array of membership roles.
    /// </summary>
    [JsonPropertyName("membership_roles")]
    public MembershipRole[] MembershipRoles { get; init; } = Array.Empty<MembershipRole>();

    /// <inheritdoc />
    public static JsonTypeInfo<OAuthUserInfo> GetTypeInfo() => OAuthUserInfoContext.Default.OAuthUserInfo;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OAuthUserInfo))]
public partial class OAuthUserInfoContext : JsonSerializerContext { }
