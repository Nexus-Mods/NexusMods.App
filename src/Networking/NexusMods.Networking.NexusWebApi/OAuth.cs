﻿using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Interprocess;
using NexusMods.Networking.NexusWebApi.Types;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// JWT Token info as provided by the OAuth server
/// </summary>
public struct JwtTokenReply
{
    /// <summary>
    /// the token to use for authentication
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }
    /// <summary>
    /// token type, e.g. "Bearer"
    /// </summary>
    [JsonPropertyName("token_type")]
    public string Type { get; set; }
    /// <summary>
    /// when the access token expires in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public ulong ExpiresIn { get; set; }
    /// <summary>
    /// token to use to refresh once this one has expired
    /// </summary>
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
    /// <summary>
    /// space separated list of scopes. defined by the server, currently always "public"?
    /// </summary>
    [JsonPropertyName("scope")]
    public string Scope { get; set; }
    /// <summary>
    /// unix timestamp (seconds resolution) of when the token was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public ulong CreatedAt { get; set; }
}

/// <summary>
/// nxm url message used in IPC. The oauth callback will spawn a new instance of NMA
/// that then needs to send the token back to the "main" process that made the request
/// </summary>
public struct NXMUrlMessage : IMessage
{
    /// <summary>
    /// the actual url
    /// </summary>
    public NXMUrl Value { get; init; }
    /// <inheritdoc/>
    public static int MaxSize { get { return 16 * 1024; } }

    /// <inheritdoc/>
    public static IMessage Read(ReadOnlySpan<byte> buffer)
    {
        var value = Encoding.UTF8.GetString(buffer);
        return new NXMUrlMessage() { Value = NXMUrl.Parse(value) };
    }

    /// <inheritdoc/>
    public int Write(Span<byte> buffer)
    {
        var buf = Encoding.UTF8.GetBytes(Value.ToString()!);
        buf.CopyTo(buffer);
        return buf.Length;
    }
}

/// <summary>
/// helper class to deal with OAuth2 authentication messages
/// </summary>
public class OAuth
{
    private static string OAuthUrl = "https://users.nexusmods.com/oauth";
    // the redirect url has to explicitly be permitted by the server so we can't change
    // this without consulting the backend team
    private static string OAuthRedirectURL = "nxm://oauth/callback";
    private static string OAuthClientId = "vortex";

    private IDictionary<string, Action<Exception?, string>> _callbacks = new Dictionary<string, Action<Exception?, string>>();
    private readonly ILogger<OAuth> _logger;
    private readonly HttpClient _http;
    private readonly IOSInterop _os;
    private readonly IIDGenerator _idGen;

    /// <summary>
    /// constructor
    /// </summary>
    public OAuth(ILogger<OAuth> logger, HttpClient http, IIDGenerator idGen, IOSInterop os, IMessageConsumer<NXMUrlMessage> message)
    {
        _logger = logger;
        _http = http;
        _os = os;
        _idGen = idGen;
        message.Messages.Subscribe(OnNXMUrl);
    }

    /// <summary>
    /// make an authorization request
    /// </summary>
    /// <param name="cancel"></param>
    /// <returns>task with the jwt token once we receive one</returns>
    public async Task<JwtTokenReply> AuthorizeRequest(CancellationToken cancel)
    {
        var completionSource = new TaskCompletionSource<JwtTokenReply>();

        var state = _idGen.UUIDv4();

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.1
        var verifier = _idGen.UUIDv4().Replace("-", "").ToBase64();
        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.2
        using SHA256 sha256 = SHA256.Create();
        var challenge = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier)).ToBase64();

        // callback will be invoked if/when we heard back from the site
        _callbacks[state] = (Exception? ex, string code) =>
            {
                if (ex != null)
                {
                    completionSource.SetException(ex);
                }
                else
                {
                    AuthorizeToken(verifier, code, CancellationToken.None).ContinueWith(reply =>
                    {
                        if (reply.Exception != null)
                        {
                            completionSource.SetException(reply.Exception);
                        }
                        else
                        {
                            completionSource.SetResult(reply.Result);
                        }
                    });
                }
            };

        cancel.Register(() => _callbacks[state].Invoke(new OperationCanceledException(), ""));

        // see https://www.rfc-editor.org/rfc/rfc7636#section-4.3
        _os.OpenURL(GenerateAuthorizeUrl(challenge, state));

        return await completionSource.Task;
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
            { "redirect_uri", OAuthRedirectURL },
            { "code", code },
            { "code_verifier", verifier }
        };

        var content = new FormUrlEncodedContent(request);

        var response = await _http.PostAsync($"{OAuthUrl}/token", content, cancel);
        var responseString = await response.Content.ReadAsStringAsync(cancel);
        return JsonSerializer.Deserialize<JwtTokenReply>(responseString);
    }

    private void OnNXMUrl(NXMUrlMessage url)
    {
        if ((url.Value.UrlType == NXMUrlType.OAuth) && (url.Value.OAuth.State != null))
        {
            if (_callbacks.ContainsKey(url.Value.OAuth.State))
            {
                _callbacks[url.Value.OAuth.State].Invoke(null, url.Value.OAuth.Code ?? "");
                _callbacks.Remove(url.Value.OAuth.State);
            }
            else
            {
                _logger.LogWarning("received unexpected oauth callback");
            }
        }
    }

    private string SanitizeBase64(string input)
    {
        return input
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }

    private string GenerateAuthorizeUrl(string challenge, string state)
    {
        var request = new Dictionary<string, string>
        {
            { "response_type", "code" },
            { "scope", "public" },
            { "code_challenge_method", "S256" },
            { "client_id", OAuthClientId },
            { "redirect_uri",  OAuthRedirectURL },
            { "code_challenge", SanitizeBase64(challenge) },
            { "state", state },
        };
        return $"{OAuthUrl}/authorize?{StringifyRequest(request)}";
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
