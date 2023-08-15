using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
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
    // the redirect url has to explicitly be permitted by the server so we can't change
    // this without consulting the backend team
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
    public OAuth(ILogger<OAuth> logger, HttpClient http, IIDGenerator idGen,
        IOSInterop os, IMessageConsumer<NXMUrlMessage> nxmUrlMessages,
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
    /// make an authorization request
    /// </summary>
    /// <param name="cancel"></param>
    /// <returns>task with the jwt token once we receive one</returns>
    public async Task<JwtTokenReply> AuthorizeRequest(CancellationToken cancel)
    {
        _logger.LogInformation("Starting NexusMods OAuth2 authorization request");
        var state = _idGen.UUIDv4();

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var verifier = _idGen.UUIDv4().Replace("-", "").ToBase64();
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        using var sha256 = SHA256.Create();
        var challenge = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier)).ToBase64();

        // Start listening first, otherwise we might miss the message
        var codeTask = _nxmUrlMessages.Messages
            .Where(url => url.Value.UrlType == NXMUrlType.OAuth)
            .Where(url => url.Value.OAuth.State == state)
            .Select(url => url.Value.OAuth.Code!)
            .ToAsyncEnumerable()
            .FirstAsync(cancel);

        _logger.LogInformation("Opening browser for NexusMods OAuth2 authorization request");
        var url = GenerateAuthorizeUrl(challenge, state);
        using var job = CreateJob(url);
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.3
        await _os.OpenUrl(url, cancel);
        var code = await codeTask;

        _logger.LogInformation("Received OAuth2 authorization code, requesting token");
        return await AuthorizeToken(verifier, code, cancel);

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

    private string SanitizeBase64(string input)
    {
        return input
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private Uri GenerateAuthorizeUrl(string challenge, string state)
    {
        var request = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "scope", "public" },
            { "code_challenge_method", "S256" },
            { "client_id", OAuthClientId },
            { "redirect_uri",  OAuthRedirectUrl },
            { "code_challenge", SanitizeBase64(challenge) },
            { "state", state },
        };
        return new Uri($"{OAuthUrl}/authorize?{StringifyRequest(request)}");
    }

    private string StringifyRequest(IDictionary<string, string> input)
    {
        IList<string> properties = new List<string>();
        foreach (var kv in input)
        {
            properties.Add($"{kv.Key}={Uri.EscapeDataString(kv.Value)}");
        }

        return string.Join("&", properties);
    }
}
