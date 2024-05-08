using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Represents a JWT Token in our DataStore.
/// </summary>
public static class JWTToken
{
    private const string Namespace = "NexusMods.Networking.NexusWebApi.Auth.JWTToken";

    /// <summary>
    /// Access token portion of the JWTToken
    /// </summary>
    public static readonly StringAttribute AccessToken = new(Namespace, nameof(AccessToken));
    
    /// <summary>
    /// Use this to get a new access token
    /// </summary>
    public static readonly StringAttribute RefreshToken = new(Namespace, nameof(RefreshToken));
    
    /// <summary>
    /// The date at which the token expires
    /// </summary>
    public static readonly TimestampAttribute ExpiresAt = new(Namespace, nameof(ExpiresAt));
    
    /// <summary>
    /// Model for the JWT Token
    /// </summary>
    /// <param name="tx"></param>
    public class Model(ITransaction tx) : Entity(tx)
    {
        /// <summary>
        /// The access token
        /// </summary>
        public string AccessToken
        {
            get => JWTToken.AccessToken.Get(this);
            set => JWTToken.AccessToken.Add(this, value);
        }
        
        /// <summary>
        /// The refresh token
        /// </summary>
        public string RefreshToken
        {
            get => JWTToken.RefreshToken.Get(this);
            set => JWTToken.RefreshToken.Add(this, value);
        }
        
        /// <summary>
        /// Expiry date of the token
        /// </summary>
        public DateTime ExpiresAt
        {
            get => JWTToken.ExpiresAt.Get(this);
            set => JWTToken.ExpiresAt.Add(this, value);
        }
        
        /// <summary>
        /// True if the token has expired.
        /// </summary>
        public bool HasExpired => ExpiresAt - TimeSpan.FromMinutes(5) <= DateTimeOffset.UtcNow;

        /// <summary>
        /// Creates a new JWT Token model from a <see cref="JwtTokenReply"/>. And reuses the existing
        /// database id if it exists, as this data is a singleton.
        /// </summary>
        public static Model? Create(IDb db, ITransaction tx, JwtTokenReply reply)
        {
            if (reply.AccessToken is null || reply.RefreshToken is null) return null;
            
            var existingId = db.Find(JWTToken.AccessToken).FirstOrDefault();
            if (existingId == EntityId.From(0))
                existingId = tx.TempId();

            return new Model(tx)
            {
                Id = existingId,
                AccessToken = reply.AccessToken,
                RefreshToken = reply.RefreshToken,
                ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(reply.CreatedAt).DateTime + TimeSpan.FromSeconds(reply.ExpiresIn),
            };
        }
    }
}


