using System.Text.Json;
using DynamicData.Kernel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Sdk;
using NexusMods.Sdk.Jobs;
using R3;

namespace NexusMods.Networking.NexusWebApi.Auth;

/// <summary>
/// helper class to deal with OAuth2 authentication messages
/// </summary>
public class OAuth
{
    // NOTE(erri120): The backend has a list of valid redirect URLs and client IDs.
    // We can't change these on our own.
    private const string OAuthRedirectUrl = "nxm://oauth/callback";
    private const string OAuthClientId = "nma";

    private readonly IJobMonitor _jobMonitor;
    private readonly ILogger<OAuth> _logger;
    private readonly HttpClient _http;
    private readonly IOSInterop _os;
    private readonly IIDGenerator _idGenerator;
    private readonly BehaviorSubject<NXMOAuthUrl?> _nxmUrlMessages = new(initialValue: null);

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth(
        IJobMonitor jobMonitor,
        ILogger<OAuth> logger,
        HttpClient http,
        IIDGenerator idGenerator,
        IOSInterop os)
    {
        _jobMonitor = jobMonitor;
        _logger = logger;
        _http = http;
        _os = os;
        _idGenerator = idGenerator;
    }

    /// <summary>
    /// Make an authorization request
    /// </summary>
    public async Task<JwtTokenReply?> AuthorizeRequest(CancellationToken cancellationToken)
    {
        var job = OAuthJob.Create(
            jobMonitor: _jobMonitor,
            idGenerator: _idGenerator,
            os: _os,
            httpClient: _http,
            nxmUrlMessages: _nxmUrlMessages.WhereNotNull()
        );

        var res = await job;
        return res.ValueOrDefault();
    }
    
    /// <summary>
    /// Add a new url as a response to an OAuth request
    /// </summary>
    public void AddUrl(NXMOAuthUrl url)
    {
        _nxmUrlMessages.OnNext(url);
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

        var response = await _http.PostAsync($"{ClientConfig.OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    internal static async Task<JwtTokenReply?> AuthorizeToken(
        string verifier,
        string code,
        HttpClient httpClient,
        CancellationToken cancel)
    {
        var request = new Dictionary<string, string> {
            { "grant_type", "authorization_code" },
            { "client_id", OAuthClientId },
            { "redirect_uri", OAuthRedirectUrl },
            { "code", code },
            { "code_verifier", verifier },
        };

        var content = new FormUrlEncodedContent(request);

        var response = await httpClient.PostAsync($"{ClientConfig.OAuthUrl}/token", content, cancel);
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

        return new Uri(QueryHelpers.AddQueryString($"{ClientConfig.OAuthUrl}/authorize", request));
    }
}
