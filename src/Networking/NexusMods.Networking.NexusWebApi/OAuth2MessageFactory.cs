using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Abstractions;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi.Types;
using System.IdentityModel.Tokens.Jwt;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NexusMods.Networking.NexusWebApi;

internal class PermissionMap
{
    [JsonPropertyName("collection")]
    public string[] Collection { get; set; } = Array.Empty<string>();

    [JsonPropertyName("collection_image")]
    public string[] CollectionImage { get; set; } = Array.Empty<string>();

    [JsonPropertyName("mod")]
    public string[] Mod { get; set; } = Array.Empty<string>();

    [JsonPropertyName("admin")]
    public string[] Admin { get; set; } = Array.Empty<string>();

    [JsonPropertyName("tag")]
    public string[] Tag { get; set; } = Array.Empty<string>();

    [JsonPropertyName("comment")]
    public string[] Comment { get; set; } = Array.Empty<string>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
internal enum MembershipRole
{
    [EnumMember(Value = "member")]
    Member,
    [EnumMember(Value = "supporter")]
    Supporter,
    [EnumMember(Value = "premium")]
    Premium,
    [EnumMember(Value = "lifetimepremium")]
    LifetimePremium,
}

internal class TokenUserInfo
{
    [JsonPropertyName("id")]
    public ulong Id { get; init; }
    [JsonPropertyName("username")]
    public string Name { get; init; } = string.Empty;
    [JsonPropertyName("group_id")]
    public int GroupId { get; init; }
    [JsonPropertyName("membership_roles")]
    public MembershipRole[] MembershipRoles { get; init; } = new MembershipRole[] { };
    [JsonPropertyName("premium_expiry")]
    public ulong PremiumExpiry { get; init; }

    [JsonPropertyName("permissions")]
    public PermissionMap Permissions { get; init; } = new PermissionMap();

}

/// <summary>
/// OAuth2 based authentification
/// </summary>
public class OAuth2MessageFactory : IAuthenticatingMessageFactory
{
    private readonly ILogger<OAuth2MessageFactory> _logger;
    private readonly IDataStore _store;
    private readonly OAuth _auth;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth2MessageFactory(ILogger<OAuth2MessageFactory> logger, IDataStore store, OAuth auth)
    {
        _logger = logger;
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
        var files = await client.ModFiles(GameDomain.From("site"), ModId.From(1), cancel);

        var tokens = _store.Get<JWTTokenEntity>(JWTTokenEntity.StoreId);
        if (tokens == null)
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        var tokenInfo = handler.ReadToken(tokens.AccessToken) as JwtSecurityToken;
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
                AccessToken = newToken.AccessToken
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
