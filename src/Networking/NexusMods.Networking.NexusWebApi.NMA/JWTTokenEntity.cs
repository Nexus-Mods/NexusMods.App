using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Abstractions.Ids;
using NexusMods.DataModel.JsonConverters;
using NexusMods.Networking.NexusWebApi.DTOs.OAuth;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// Represents a JWT Token in our DataStore.
/// </summary>
[JsonName("JWTTokens")]
public record JWTTokenEntity : Entity
{
    /// <summary>
    /// ID used to refer to JWT tokens in the data store.
    /// </summary>
    public static readonly IId StoreId = new IdVariableLength(EntityCategory.AuthData, "NexusMods.Networking.NexusWebApi.JWTTokens"u8.ToArray());

    /// <summary>
    /// Creates a new <see cref="JWTTokenEntity"/> from a <see cref="JwtTokenReply"/>.
    /// </summary>
    public static JWTTokenEntity? From(JwtTokenReply? tokenReply)
    {
        if (tokenReply?.AccessToken is null || tokenReply.RefreshToken is null) return null;
        return new JWTTokenEntity
        {
            AccessToken = tokenReply.AccessToken,
            RefreshToken = tokenReply.RefreshToken,
            ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(tokenReply.CreatedAt) + TimeSpan.FromSeconds(tokenReply.ExpiresIn),
            DataStoreId = StoreId
        };
    }

    /// <inheritdoc/>
    public override EntityCategory Category => StoreId.Category;

    /// <summary>
    /// Gets the access token.
    /// </summary>
    /// <remarks>
    /// This token expires at <see cref="ExpiresAt"/> and needs to be refreshed using <see cref="RefreshToken"/>.
    /// </remarks>
    public required string AccessToken { get; init; }

    /// <summary>
    /// Gets the refresh token.
    /// </summary>
    public required string RefreshToken { get; init; }

    /// <summary>
    /// Gets the date at which the <see cref="AccessToken"/> expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Checks whether the token has expired.
    /// </summary>
    public bool HasExpired()
    {
        return ExpiresAt - TimeSpan.FromMinutes(5) <= DateTimeOffset.UtcNow;
    }
}


