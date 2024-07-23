using System.Diagnostics.CodeAnalysis;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using File = NexusMods.Abstractions.Loadouts.Files.File;

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
    
    
    /// <summary>
    /// Try to find the JWT Token in the database.
    /// </summary>
    public static bool TryFind(IDb db, out ReadOnly token)
    {
        var found = All(db).FirstOrDefault();
        if (found.IsValid())
        {
            token = found;
            return true;
        }
        
        token = default(ReadOnly);
        return false;
    }
    
    
    /// <summary>
    /// Creates a new JWT Token model from a <see cref="JwtTokenReply"/>. And reuses the existing
    /// database id if it exists, as this data is a singleton.
    /// </summary>
    public static EntityId? Create(IDb db, ITransaction tx, JwtTokenReply reply)
    {
        if (reply.AccessToken is null || reply.RefreshToken is null) 
            return null;
            
        var existingId = db.Datoms(JWTToken.AccessToken).FirstOrDefault().E;
        if (existingId == EntityId.From(0))
            existingId = tx.TempId();
        
        tx.Add(existingId, JWTToken.AccessToken, reply.AccessToken);
        tx.Add(existingId, JWTToken.RefreshToken, reply.RefreshToken);
        tx.Add(existingId, JWTToken.ExpiresAt, DateTimeOffset.FromUnixTimeSeconds(reply.CreatedAt).DateTime + TimeSpan.FromSeconds(reply.ExpiresIn));

        return existingId;
    }
    
    /// <summary>
    /// Model for the JWT Token
    /// </summary>
    /// <param name="tx"></param>
    public partial struct ReadOnly
    {
        /// <summary>
        /// True if the token has expired.
        /// </summary>
        public bool HasExpired => ExpiresAt - TimeSpan.FromMinutes(5) <= DateTimeOffset.UtcNow;
    }
}


