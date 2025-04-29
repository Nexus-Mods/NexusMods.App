using NexusMods.MnemonicDB.Abstractions.Attributes;
using NexusMods.MnemonicDB.Abstractions.Models;

namespace NexusMods.Networking.GOG.Models;

public partial class AuthInfo : IModelDefinition
{
    public const string Namespace = "NexusMods.Networking.GOG.AuthInfo";
    
    /// <summary>
    /// The time at which the token expires.
    /// </summary>
    public static readonly TimestampAttribute ExpiresAt = new(Namespace, nameof(ExpiresAt));
    
    /// <summary>
    /// The access token.
    /// </summary>
    public static readonly StringAttribute AccessToken = new(Namespace, nameof(AccessToken));
    
    /// <summary>
    /// The session id.
    /// </summary>
    public static readonly StringAttribute SessionId = new(Namespace, nameof(SessionId));
    
    /// <summary>
    /// The refresh token.
    /// </summary>
    public static readonly StringAttribute RefreshToken = new(Namespace, nameof(RefreshToken));
    
    /// <summary>
    /// The User ID.
    /// </summary>
    public static readonly UInt64Attribute UserId = new(Namespace, nameof(UserId));
    
}
