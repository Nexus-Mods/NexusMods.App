using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.OSInterop;
using NexusMods.DataModel.Interprocess;
using NexusMods.DataModel.Interprocess.Jobs;
using NexusMods.Networking.NexusWebApi.DTOs.OAuth;
using NexusMods.Networking.NexusWebApi.NMA.Messages;
using NexusMods.Networking.NexusWebApi.NMA.Types;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.NMA;

/// <summary>
/// helper class to deal with OAuth2 authentication messages
/// </summary>
public class OAuth
{
    private const string OAuthUrl = "https://users.nexusmods.com/oauth";
    // NOTE(erri120): The backend has a list of valid redirect URLs and client IDs.
    // We can't change these on our own.
    private const string OAuthRedirectUrl = "nxm://oauth/callback";
    private const string OAuthClientId = "vortex";

    private readonly ILogger<OAuth> _logger;
    private readonly HttpClient _http;
    private readonly IOSInterop _os;
    private readonly IIDGenerator _idGen;
    private readonly IMessageConsumer<NXMUrlMessage> _nxmUrlMessages;
    private readonly IInterprocessJobManager _jobManager;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth(ILogger<OAuth> logger,
        HttpClient http,
        IIDGenerator idGen,
        IOSInterop os,
        IMessageConsumer<NXMUrlMessage> nxmUrlMessages,
        IInterprocessJobManager jobManager)
    {
        _logger = logger;
        _http = http;
        _os = os;
        _idGen = idGen;
        _jobManager = jobManager;
        _nxmUrlMessages = nxmUrlMessages;
    }

    /// <summary>
    /// Make an authorization request
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns>task with the jwt token once we receive one</returns>
    public async Task<JwtTokenReply> AuthorizeRequest(CancellationToken cancellationToken)
    {
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var codeVerifier = _idGen.UUIDv4().ToBase64();

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        var codeChallengeBytes = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier));
        var codeChallenge = StringEncodingExtension.Base64UrlEncode(codeChallengeBytes);

        var state = _idGen.UUIDv4();

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

    private IInterprocessJob CreateJob(Uri url)
    {
        return InterprocessJob.Create(_jobManager, new NexusLoginJob {Uri = url});
    }

    /// <summary>
    /// request a new access token
    /// </summary>
    /// <param name="refreshToken">the refresh token</param>
    /// <param name="cancel"></param>
    /// <returns>a new token reply</returns>
    public async Task<JwtTokenReply> RefreshToken(string refreshToken, CancellationToken cancel)
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

    private async Task<JwtTokenReply> AuthorizeToken(string verifier, string code, CancellationToken cancel)
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
        // TODO: switch to Microsoft.AspNetCore.WebUtilities when .NET 8 is available
        var request = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "scope", "public" },
            { "code_challenge_method", "S256" },
            { "client_id", OAuthClientId },
            { "redirect_uri",  OAuthRedirectUrl },
            { "code_challenge", challenge },
            { "state", state },
        };

        return new Uri($"{OAuthUrl}/authorize{CreateQueryString(request)}");
    }

    private static string CreateQueryString(Dictionary<string, string> parameters)
    {
        var builder = new StringBuilder();
        var first = true;
        foreach (var pair in parameters)
        {
            var (key, value) = pair;

            builder.Append(first ? '?' : '&');
            builder.Append(UrlEncoder.Default.Encode(key));
            builder.Append('=');
            if (!string.IsNullOrEmpty(value))
            {
                builder.Append(UrlEncoder.Default.Encode(value));
            }

            first = false;
        }

        return builder.ToString();
    }
}
