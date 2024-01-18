using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using JetBrains.Annotations;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;

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
    public string Sub { get; set; } = string.Empty;

    /// <summary>
    /// Gets the User Name.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets the avatar url.
    /// </summary>
    [JsonPropertyName("avatar")]
    public Uri? Avatar { get; set; }

    /// <summary>
    /// Gets an array of membership roles.
    /// </summary>
    [JsonPropertyName("membership_roles")]
    public MembershipRole[] MembershipRoles { get; set; } = Array.Empty<MembershipRole>();

    /// <inheritdoc />
    public static JsonTypeInfo<OAuthUserInfo> GetTypeInfo() => OAuthUserInfoContext.Default.OAuthUserInfo;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(OAuthUserInfo))]
public partial class OAuthUserInfoContext : JsonSerializerContext { }
