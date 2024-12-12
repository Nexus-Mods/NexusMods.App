using System.Text.Json.Serialization;

namespace NexusMods.Networking.GOG.DTOs;

/// <summary>
/// An Authentication Token response from the GOG API.
/// </summary>
internal class TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }
    
    [JsonPropertyName("token_type")]
    public required string TokenType { get; init; }
    
    [JsonPropertyName("expires_in")]
    public required int ExpiresIn { get; init; }
    
    [JsonPropertyName("refresh_token")]
    public required string RefreshToken { get; init; }
    
    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
    
    [JsonPropertyName("session_id")]
    public required string SessionId { get; init; }
    
    [JsonPropertyName("user_id")]
    public required string UserId { get; init; }
}
