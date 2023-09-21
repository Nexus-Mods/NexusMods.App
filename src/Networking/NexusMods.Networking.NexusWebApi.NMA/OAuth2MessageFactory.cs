using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.Networking.NexusWebApi.DTOs.OAuth;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// OAuth2 based authentication
/// </summary>
public class OAuth2MessageFactory : IAuthenticatingMessageFactory
{
    private readonly ILogger<OAuth2MessageFactory> _logger;
    private readonly IDataStore _store;
    private readonly OAuth _auth;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    /// <summary>
    /// Constructor.
    /// </summary>
    public OAuth2MessageFactory(
        IDataStore store,
        OAuth auth,
        ILogger<OAuth2MessageFactory> logger)
    {
        _store = store;
        _auth = auth;
        _logger = logger;
    }

    // TODO: listen to DB changes when the user wants to logout
    private JWTTokenEntity? _cachedTokenEntity;
    private UserInfo? _cachedUserInfo;

    private async ValueTask<string?> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        _cachedTokenEntity ??= _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
        if (_cachedTokenEntity is null) return null;
        if (!_cachedTokenEntity.HasExpired()) return _cachedTokenEntity.AccessToken;

        _logger.LogDebug("Refreshing expired OAuth token");

        var newToken = await _auth.RefreshToken(_cachedTokenEntity.RefreshToken, cancellationToken);
        _cachedTokenEntity = new JWTTokenEntity
        {
            RefreshToken = newToken.RefreshToken,
            AccessToken = newToken.AccessToken,
            ExpiresAt = DateTimeOffset.UtcNow
        };

        _cachedUserInfo = null;
        _store.Put(JWTTokenEntity.StoreId, _cachedTokenEntity);
        return _cachedTokenEntity.AccessToken;
    }

    /// <inheritdoc/>
    public async ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {
        var token = await GetOrRefreshToken(CancellationToken.None);
        if (token is null) throw new Exception("Unauthorized!");

        var msg = new HttpRequestMessage(method, uri);
        msg.Headers.Add("Authorization", $"Bearer {token}");

        return msg;
    }

    /// <inheritdoc/>
    public async ValueTask<bool> IsAuthenticated()
    {
        var token = await GetOrRefreshToken(CancellationToken.None);
        return token is not null;
    }

    /// <inheritdoc/>
    public async ValueTask<UserInfo?> Verify(Client client, CancellationToken cancel)
    {
        if (_cachedUserInfo is not null) return _cachedUserInfo;
        _logger.LogDebug("Renewing cached user info");

        var token = await GetOrRefreshToken(cancellationToken: cancel);

        var tokenInfo = _tokenHandler.ReadJwtToken(token);
        var userClaim = tokenInfo?.Claims.FirstOrDefault(iter => iter.Type == "user");
        var userInfo = userClaim is not null ? JsonSerializer.Deserialize<TokenUserInfo>(userClaim.Value) : null;

        if (userInfo is null)
        {
            _logger.LogError("Unable to extract user info from token!");
            return null;
        }

        _cachedUserInfo = new UserInfo
        {
            Name = userInfo.Name,
            IsPremium = userInfo.MembershipRoles.Contains(MembershipRole.Premium),
            IsSupporter = userInfo.MembershipRoles.Contains(MembershipRole.Supporter),
            // TODO: fetch avatar
        };

        return _cachedUserInfo;
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel)
    {
        return ValueTask.FromResult<HttpRequestMessage?>(null);
    }
}
