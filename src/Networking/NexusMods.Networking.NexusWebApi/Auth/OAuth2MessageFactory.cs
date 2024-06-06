using System.Reactive.Linq;
using DynamicData.Binding;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// OAuth2 based authentication
/// </summary>
public class OAuth2MessageFactory : IAuthenticatingMessageFactory
{
    private readonly ILogger<OAuth2MessageFactory> _logger;
    private readonly OAuth _auth;

    /// <summary>
    /// Constructor.
    /// </summary>
    public OAuth2MessageFactory(
        IConnection conn,
        IRepository<JWTToken.Model> jwtTokenRepository,
        OAuth auth,
        ILogger<OAuth2MessageFactory> logger)
    {
        _conn = conn;
        _jwtTokenRepository = jwtTokenRepository;
        _auth = auth;
        _logger = logger;

        jwtTokenRepository.Observable
            .ToObservableChangeSet()
            .Subscribe(_ => _cachedTokenEntity = null);
    }

    private JWTToken.Model? _cachedTokenEntity;
    private readonly IConnection _conn;
    private readonly IRepository<JWTToken.Model> _jwtTokenRepository;

    private async ValueTask<string?> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        if (!_jwtTokenRepository.TryFindFirst(out var token))
            return null;
        
        _cachedTokenEntity = token;
        if (!_cachedTokenEntity.HasExpired) return _cachedTokenEntity.AccessToken;

        _logger.LogDebug("Refreshing expired OAuth token");

        var newToken = await _auth.RefreshToken(_cachedTokenEntity.RefreshToken, cancellationToken);
        var db = _conn.Db;
        using var tx = _conn.BeginTransaction();
        
        var newTokenEntity = JWTToken.Model.Create(db, tx, newToken!);
        if (newTokenEntity is null)
        {
            _logger.LogError("Invalid new token in OAuth2MessageFactory");
            return null;
        }

        var result = await tx.Commit();
        newTokenEntity = result.Remap(newTokenEntity);

        _cachedTokenEntity = newTokenEntity;
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
            UserId = UserId.From(ulong.Parse(oAuthUserInfo.Sub)),
            Name = oAuthUserInfo.Name,
            IsPremium = oAuthUserInfo.MembershipRoles.Contains(MembershipRole.Premium),
            AvatarUrl = oAuthUserInfo.Avatar,
        };
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel)
    {
        return ValueTask.FromResult<HttpRequestMessage?>(null);
    }
}
