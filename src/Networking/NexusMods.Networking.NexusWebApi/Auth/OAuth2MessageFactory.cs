using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// OAuth2 based authentication
/// </summary>
public class OAuth2MessageFactory : IAuthenticatingMessageFactory
{
    private readonly ILogger<OAuth2MessageFactory> _logger;
    private readonly IDataStore _store;
    private readonly OAuth _auth;

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

        _store.IdChanges
            .Where(x => x.Equals(JWTTokenEntity.StoreId))
            .Subscribe(_ => _cachedTokenEntity = null);
    }

    private JWTTokenEntity? _cachedTokenEntity;

    private async ValueTask<string?> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        _cachedTokenEntity ??= _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
        if (_cachedTokenEntity is null) return null;
        if (!_cachedTokenEntity.HasExpired()) return _cachedTokenEntity.AccessToken;

        _logger.LogDebug("Refreshing expired OAuth token");

        var newToken = await _auth.RefreshToken(_cachedTokenEntity.RefreshToken, cancellationToken);
        var newTokenEntity = JWTTokenEntity.From(newToken);
        if (newTokenEntity is null)
        {
            _logger.LogError("Invalid new token!");
            return null;
        }

        _cachedTokenEntity = newTokenEntity;
        _store.Put(JWTTokenEntity.StoreId, _cachedTokenEntity);
        _cachedTokenEntity.DataStoreId = JWTTokenEntity.StoreId;

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
    public async ValueTask<UserInfo?> Verify(INexusApiClient nexusApiNexusApiClient, CancellationToken cancel)
    {
        OAuthUserInfo oAuthUserInfo;
        try
        {
            var res = await nexusApiNexusApiClient.GetOAuthUserInfo(cancellationToken: cancel);
            oAuthUserInfo = res.Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while fetching OAuth user info");
            return null;
        }

        return new UserInfo
        {
            Name = oAuthUserInfo.Name,
            IsPremium = oAuthUserInfo.MembershipRoles.Contains(MembershipRole.Premium),
            AvatarUrl = oAuthUserInfo.Avatar
        };
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel)
    {
        return ValueTask.FromResult<HttpRequestMessage?>(null);
    }
}
