using System.Text.Json.Serialization;

namespace NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;

/// <summary>
/// JWT Token info as provided by the OAuth server
/// </summary>
public class JwtTokenReply
{
    /// <summary>
    /// the token to use for authentication
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// token type, e.g. "Bearer"
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? Type { get; set; }

    /// <summary>
    /// when the access token expires in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public ulong ExpiresIn { get; set; }

    /// <summary>
    /// token to use to refresh once this one has expired
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// space separated list of scopes. defined by the server, currently always "public"?
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// unix timestamp (seconds resolution) of when the token was created.
    /// This timestamp is UTC+0.
    /// </summary>
    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; }
}
