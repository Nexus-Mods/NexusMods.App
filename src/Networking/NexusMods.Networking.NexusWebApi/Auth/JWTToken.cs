using DynamicData.Kernel;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// Represents a JWT Token in our DataStore.
/// </summary>
public partial class JWTToken : IModelDefinition
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

    private static Optional<EntityId> GetEntityId(IDb db)
    {
        var datoms = db.Datoms(PrimaryAttribute);
        return datoms.Count == 0 ? Optional<EntityId>.None : datoms[0].E;
    }

    /// <summary>
    /// Try to find the JWT Token in the database.
    /// </summary>
    public static bool TryFind(IDb db, out ReadOnly token)
    {
        var entityId = GetEntityId(db);
        if (!entityId.HasValue)
        {
            token = default(ReadOnly);
            return false;
        }

        token = Load(db, entityId.Value);
        return token.IsValid();
    }

    /// <summary>
    /// Creates a new JWT Token model from a <see cref="JwtTokenReply"/>. And reuses the existing
    /// database id if it exists, as this data is a singleton.
    /// </summary>
    public static Optional<EntityId> Create(IDb db, ITransaction tx, JwtTokenReply reply)
    {
        if (reply.AccessToken is null || reply.RefreshToken is null) return Optional<EntityId>.None;

        var existingId = GetEntityId(db);
        var entityId = existingId.HasValue ? existingId.Value : tx.TempId();

        tx.Add(entityId, AccessToken, reply.AccessToken);
        tx.Add(entityId, RefreshToken, reply.RefreshToken);
        tx.Add(entityId, ExpiresAt, DateTimeOffset.FromUnixTimeSeconds(reply.CreatedAt) + TimeSpan.FromSeconds(reply.ExpiresIn));

        return entityId;
    }
    
    /// <summary>
    /// Model for the JWT Token
    /// </summary>
    public partial struct ReadOnly
    {
        /// <summary>
        /// True if the token has expired.
        /// </summary>
        public bool HasExpired => ExpiresAt - TimeSpan.FromMinutes(5) <= DateTimeOffset.UtcNow;
    }
}


