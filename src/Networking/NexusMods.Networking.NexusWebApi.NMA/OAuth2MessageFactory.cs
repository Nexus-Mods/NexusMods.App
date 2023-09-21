using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi.DTOs.OAuth;
using NexusMods.Networking.NexusWebApi.NMA.Extensions;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// OAuth2 based authentication
/// </summary>
public class OAuth2MessageFactory : IAuthenticatingMessageFactory
{
    private readonly IDataStore _store;
    private readonly OAuth _auth;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth2MessageFactory(IDataStore store, OAuth auth)
    {
        _store = store;
        _auth = auth;
    }

    /// <inheritdoc/>
    public ValueTask<HttpRequestMessage> Create(HttpMethod method, Uri uri)
    {/*
        if (uri.LocalPath == "/v1/users/validate.json")
        {
            // we shouldn't have tried to call this
            throw new Exception("the validate endpoint does not work when using oauth");
        }*/
        var msg = new HttpRequestMessage(method, uri);
        msg.Headers.Add("Authorization", $"Bearer {Token}");
        return ValueTask.FromResult(msg);
    }

    /// <inheritdoc/>
    public async ValueTask<bool> IsAuthenticated()
    {
        var token = _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
        return await ValueTask.FromResult(token != null);
    }

    /// <inheritdoc/>
    public async ValueTask<UserInfo?> Verify(Client client, CancellationToken cancel)
    {
        // TODO: there is no dedicated api endpoint we can call just to check the oauth token.
        //   Once graphql is implemented I recommend to fetch user info about the user the token belongs to
        //   so we can report more details about them.
        //   For now, any query will fail if the token is valid
        await client.ModFilesAsync(GameDomain.From("site"), ModId.From(1), cancel);

        var tokens = _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
        if (tokens == null)
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        var tokenInfo = handler.ReadJwtToken(tokens.AccessToken);
        var userClaim = tokenInfo?.Claims.FirstOrDefault(iter => iter.Type == "user");
        var userInfo = userClaim != null ? JsonSerializer.Deserialize<TokenUserInfo>(userClaim.Value) : null;

        if (userInfo == null)
        {
            // we have a token but it's invalid, should we report this or assume it's the server's fault?
            return null;
        }

        return new UserInfo
        {
            Name = userInfo.Name,
            IsPremium = userInfo.MembershipRoles.Contains(MembershipRole.Premium),
            IsSupporter = userInfo.MembershipRoles.Contains(MembershipRole.Supporter),
            Avatar = new Uri($"https://forums.nexusmods.com/uploads/profile/photo-thumb-{userInfo.Id}.png")
        };
    }

    /// <summary>
    /// will handle "Token has expired" errors by refreshing the JWT token and then triggering a retry of the same
    /// query
    /// </summary>
    public async ValueTask<HttpRequestMessage?> HandleError(HttpRequestMessage original, HttpRequestException ex, CancellationToken cancel)
    {
        if ((ex.StatusCode == System.Net.HttpStatusCode.Unauthorized) && (ex.Message == "Token has expired"))
        {
            var tokens = _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
            if (tokens == null)
            {
                // this shouldn't be possible, why would we get "Token has expired" if we don't _have_ a token?
                // unless there is a race condition whereby another thread has deleted the token after the request was made
                return null;
            }
            var newToken = await _auth.RefreshToken(tokens.RefreshToken, cancel);

            _store.Put(JWTTokenEntity.StoreId, new JWTTokenEntity
            {
                RefreshToken = newToken.RefreshToken,
                AccessToken = newToken.AccessToken,
                ExpiresAt = DateTimeOffset.UtcNow
            });

            var msg = new HttpRequestMessage(original.Method, original.RequestUri);
            msg.Headers.Add("Authorization", $"Bearer {newToken.AccessToken}");
            return msg;
        }

        return null;
    }

    private string Token
    {
        get
        {
            var token = _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
            var value = token?.AccessToken ?? null;
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new Exception("No OAuth2 token");
            }

            return value;
        }
    }
}
