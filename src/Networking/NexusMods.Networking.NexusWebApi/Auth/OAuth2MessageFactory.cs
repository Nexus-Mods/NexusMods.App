using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
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
        OAuth auth,
        ILogger<OAuth2MessageFactory> logger)
    {
        _conn = conn;
        _auth = auth;
        _logger = logger;
    }

    private readonly IConnection _conn;

    private async ValueTask<string?> GetOrRefreshToken(CancellationToken cancellationToken)
    {
        if (!JWTToken.TryFind(_conn.Db, out var token)) return null;
        if (!token.HasExpired) return token.AccessToken;

        _logger.LogDebug("Refreshing expired OAuth token");

        var newToken = await _auth.RefreshToken(token.RefreshToken, cancellationToken);
        var db = _conn.Db;
        using var tx = _conn.BeginTransaction();

        var newTokenEntity = JWTToken.Create(db, tx, newToken!);
        if (!newTokenEntity.HasValue)
        {
            _logger.LogError("Invalid new token in OAuth2MessageFactory");
            return null;
        }

        var result = await tx.Commit();

        token = JWTToken.Load(result.Db, result[newTokenEntity.Value]);
        return token.AccessToken;
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
            UserRole =  oAuthUserInfo.MembershipRoles.Contains(MembershipRole.Premium) ? UserRole.Premium : oAuthUserInfo.MembershipRoles.Contains(MembershipRole.Supporter) ? UserRole.Supporter : UserRole.Free,
            AvatarUrl = oAuthUserInfo.Avatar,
        };
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel)
    {
        return ValueTask.FromResult<HttpRequestMessage?>(null);
    }
}
