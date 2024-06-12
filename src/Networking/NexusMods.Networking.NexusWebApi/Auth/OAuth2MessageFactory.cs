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
using NexusMods.MnemonicDB.Abstractions.Query;

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
        OAuth auth,
        ILogger<OAuth2MessageFactory> logger)
    {
        _conn = conn;
        _auth = auth;
        _logger = logger;
        
        _conn.ObserveDatoms(SliceDescriptor.Create(JWTToken.AccessToken, _conn.Registry))
            .Subscribe(_ => _cachedTokenEntity = null);
    }

    private JWTToken.ReadOnly? _cachedTokenEntity;
    private readonly IConnection _conn;

    private async ValueTask<string?> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        if (!JWTToken.TryFind(_conn.Db, out var token))
            return null;
        
        _cachedTokenEntity = token;
        if (!token.HasExpired) 
            return _cachedTokenEntity!.Value.AccessToken;

        _logger.LogDebug("Refreshing expired OAuth token");

        var newToken = await _auth.RefreshToken(token.RefreshToken, cancellationToken);
        var db = _conn.Db;
        using var tx = _conn.BeginTransaction();
        
        var newTokenEntity = JWTToken.Create(db, tx, newToken!);
        if (newTokenEntity is null)
        {
            _logger.LogError("Invalid new token in OAuth2MessageFactory");
            return null;
        }

        var result = await tx.Commit();

        _cachedTokenEntity = JWTToken.Load(result.Db, result[newTokenEntity.Value]);
        return _cachedTokenEntity!.Value.AccessToken;
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
