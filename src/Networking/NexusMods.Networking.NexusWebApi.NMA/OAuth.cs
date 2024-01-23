using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Activities;
using NexusMods.Abstractions.Messaging;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.BCL.Extensions;
using NexusMods.CrossPlatform.Process;
using NexusMods.Networking.NexusWebApi.NMA.Messages;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// helper class to deal with OAuth2 authentication messages
/// </summary>
public class OAuth
{
    /// <summary>
    /// The activity group for all activities related to OAuth.
    /// </summary>
    public static readonly ActivityGroup Group = ActivityGroup.From("OAuth");

    private const string OAuthUrl = "https://users.nexusmods.com/oauth";
    // NOTE(erri120): The backend has a list of valid redirect URLs and client IDs.
    // We can't change these on our own.
    private const string OAuthRedirectUrl = "nxm://oauth/callback";
    private const string OAuthClientId = "nma";

    private readonly ILogger<OAuth> _logger;
    private readonly HttpClient _http;
    private readonly IOSInterop _os;
    private readonly IIDGenerator _idGenerator;
    private readonly IMessageConsumer<NXMUrlMessage> _nxmUrlMessages;
    private readonly IActivityFactory _activityFactory;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth(ILogger<OAuth> logger,
        HttpClient http,
        IIDGenerator idGenerator,
        IOSInterop os,
        IMessageConsumer<NXMUrlMessage> nxmUrlMessages,
        IActivityFactory activityFactory)
    {
        _logger = logger;
        _http = http;
        _os = os;
        _idGenerator = idGenerator;
        _activityFactory = activityFactory;
        _nxmUrlMessages = nxmUrlMessages;
    }

    /// <summary>
    /// Make an authorization request
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <param name="state">Pre-computed 'state' value for the authorize URL.</param>
    /// <returns>task with the jwt token once we receive one</returns>
    public async Task<JwtTokenReply?> AuthorizeRequest(CancellationToken cancellationToken)
    {
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var codeVerifier = _idGenerator.UUIDv4().ToBase64();

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        var codeChallengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = StringBase64Extensions.Base64UrlEncode(codeChallengeBytes);

        var state = _idGenerator.UUIDv4();

        // Start listening first, otherwise we might miss the message
        var codeTask = _nxmUrlMessages.Messages
            .Where(url => url.Value.UrlType == NXMUrlType.OAuth && url.Value.OAuth.State == state)
            .Select(url => url.Value.OAuth.Code)
            .Where(code => code is not null)
            .Select(code => code!)
            .ToAsyncEnumerable()
            .FirstAsync(cancellationToken);

        var url = GenerateAuthorizeUrl(codeChallenge, state);
        using var job = CreateJob(url);

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.3
        await _os.OpenUrl(url, cancellationToken);
        var code = await codeTask;

        return await AuthorizeToken(codeVerifier, code, cancellationToken);
    }

    private IActivitySource CreateJob(Uri url)
    {
        return _activityFactory.CreateWithPayload(Group, url, "Logging into Nexus Mods, redirecting to {Url}", url);
    }

    /// <summary>
    /// request a new access token
    /// </summary>
    /// <param name="refreshToken">the refresh token</param>
    /// <param name="cancel"></param>
    /// <returns>a new token reply</returns>
    public async Task<JwtTokenReply?> RefreshToken(string refreshToken, CancellationToken cancel)
    {
        var request = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "client_id", OAuthClientId },
            { "refresh_token", refreshToken },
        };

        var content = new FormUrlEncodedContent(request);

        var response = await _http.PostAsync($"{OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    private async Task<JwtTokenReply?> AuthorizeToken(string verifier, string code, CancellationToken cancel)
    {
        var request = new Dictionary<string, string> {
            { "grant_type", "authorization_code" },
            { "client_id", OAuthClientId },
            { "redirect_uri", OAuthRedirectUrl },
            { "code", code },
            { "code_verifier", verifier }
        };

        var content = new FormUrlEncodedContent(request);

        var response = await _http.PostAsync($"{OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    internal static Uri GenerateAuthorizeUrl(string challenge, string state)
    {
        var request = new Dictionary<string, string?>
        {
            { "response_type", "code" },
            { "scope", "openid profile email" },
            { "code_challenge_method", "S256" },
            { "client_id", OAuthClientId },
            { "redirect_uri",  OAuthRedirectUrl },
            { "code_challenge", challenge },
            { "state", state },
        };

        return new Uri(QueryHelpers.AddQueryString($"{OAuthUrl}/authorize", request));
    }
}
