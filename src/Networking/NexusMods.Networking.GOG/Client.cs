using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using DynamicData.Kernel;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.GOG.DTOs;
using NexusMods.Networking.GOG.Models;

namespace NexusMods.Networking.GOG;

public class Client
{
    private readonly IOAuthUserInterventionHandler _oAuthUserInterventionHandler;
    private readonly ILogger<Client> _logger;
    private readonly IConnection _connection;
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private const string _clientId = "46899977096215655";
    private const string _clientSecret = "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9";

    private static Uri _authorizationUri =
        new("https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri=https://embed.gog.com/on_login_success?origin=client&response_type=code&layout=galaxy");

    private static Uri _callbackPrefix = new("https://embed.gog.com/on_login_success");
    
    private static Uri _tokenUri = new("https://auth.gog.com/token");
    

    public Client(ILogger<Client> logger, IConnection connection, IOAuthUserInterventionHandler oAuthUserInterventionHandler, HttpClient client, JsonSerializerOptions jsonSerializerOptions)
    {
        _client = client;
        _logger = logger;
        _connection = connection;
        _oAuthUserInterventionHandler = oAuthUserInterventionHandler;
        
        _jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };
        
    }


    public async Task Login(CancellationToken token)
    {
        var request = await _oAuthUserInterventionHandler.HandleOAuthRequest(new OAuthLoginRequest
            {
                AuthorizationUrl = _authorizationUri,
                CallbackType = CallbackType.Capture,
                CallbackPrefix = _callbackPrefix,
            }
        , token);
        if (request == null)
        {
            _logger.LogWarning("The OAuth login request was cancelled.");
            return;
        }
        
        var parsed = HttpUtility.ParseQueryString(request.Query);
        var code = parsed["code"];
        if (code == null)
        {
            _logger.LogError("The OAuth login request did not contain a code.");
            return;
        }

        var tokenQuery = new Dictionary<string, string?>()
        {
            { "client_id", _clientId },
            { "client_secret", _clientSecret },
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", "https://embed.gog.com/on_login_success?origin=client" },
        };
        
        var uri = new Uri(QueryHelpers.AddQueryString(_tokenUri.ToString(), tokenQuery));
        
        var tokenResponse = await _client.GetFromJsonAsync<TokenResponse>(uri, token);
        
        if (tokenResponse == null)
        {
            _logger.LogError("The OAuth login request did not return a token.");
            return;
        }
        
        using var tx = _connection.BeginTransaction();
        var e = tx.TempId();
        if (TryGetAuthInfo(out var found))
            e = found.Id;
        
        tx.Add(e, AuthInfo.AccessToken, tokenResponse.AccessToken);
        tx.Add(e, AuthInfo.RefreshToken, tokenResponse.RefreshToken);
        tx.Add(e, AuthInfo.ExpriesAt, DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
        tx.Add(e, AuthInfo.SessionId, tokenResponse.SessionId);
        tx.Add(e, AuthInfo.UserId, ulong.Parse(tokenResponse.UserId));
        await tx.Commit();
        _logger.LogInformation("Logged in successfully to GOG.");
    }

    private async ValueTask<HttpRequestMessage> CreateMessage(Uri uri, CancellationToken token)
    {
        if (!TryGetAuthInfo(out var authInfo))
            throw new InvalidOperationException("No authentication information found.");

        Console.WriteLine(authInfo.AccessToken);
        if (NeedsRefresh(authInfo))
            authInfo = await RefreshToken(token);
        
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.Authorization = new("Bearer", authInfo.AccessToken);
        return message;
    }
    
    private bool NeedsRefresh(AuthInfo.ReadOnly authInfo)
    {
        return authInfo.ExpriesAt < DateTimeOffset.UtcNow - TimeSpan.FromMinutes(1);
    }

    private async Task<AuthInfo.ReadOnly> RefreshToken(CancellationToken token)
    {
        // Lock so we don't refresh multiple times
        await _semaphore.WaitAsync();
        try
        {
            if (!TryGetAuthInfo(out var authInfo))
                throw new InvalidOperationException("No authentication information found.");

            if (!NeedsRefresh(authInfo))
                return authInfo;

            var tokenQuery = new Dictionary<string, string?>
            {
                { "client_id", _clientId },
                { "client_secret", _clientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", authInfo.RefreshToken },
            };
        
            var uri = new Uri(QueryHelpers.AddQueryString(_tokenUri.ToString(), tokenQuery));
        
            var tokenResponse = await _client.GetFromJsonAsync<TokenResponse>(uri, token);
            if (tokenResponse == null)
            {
                throw new Exception("The OAuth login request did not return a token.");
            }
        
            using var tx = _connection.BeginTransaction();
            var e = authInfo.Id;
            tx.Add(e, AuthInfo.AccessToken, tokenResponse.AccessToken);
            tx.Add(e, AuthInfo.RefreshToken, tokenResponse.RefreshToken);
            tx.Add(e, AuthInfo.ExpriesAt, DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
            tx.Add(e, AuthInfo.SessionId, tokenResponse.SessionId);
            tx.Add(e, AuthInfo.UserId, ulong.Parse(tokenResponse.UserId));
            var result = await tx.Commit();
            return authInfo.Rebase(result.Db);
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    /// <summary>
    /// Try to get the authentication information from the database.
    /// </summary>
    public bool TryGetAuthInfo(out AuthInfo.ReadOnly authInfo)
    {
        foreach (var found in AuthInfo.All(_connection.Db))
        {
            authInfo = found;
            return true;
        }
        authInfo = default(AuthInfo.ReadOnly);
        return false;
    }

    private class BuildList
    {
        [JsonPropertyName("items")]
        public required Build[] Items { get; init; }
    }
    
    public async Task<Build[]> GetBuilds(ProductId productId, OS os, CancellationToken token)
    {
        var msg = await CreateMessage(new Uri($"https://content-system.gog.com/products/{productId}/os/{os}/builds?generation=2"), CancellationToken.None);
        using var response = await _client.SendAsync(msg, token);
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Failed to get builds for {productId} on {os}. {response.StatusCode}");

        await using var responseStream = await response.Content.ReadAsStreamAsync(token);
        var content = await JsonSerializer.DeserializeAsync<BuildList>(responseStream, _jsonSerializerOptions, token);
        
        if (content == null)
            throw new Exception("Failed to deserialize the builds response.");
        
        return content.Items;
    }

    /// <summary>
    /// Try to log out the user.
    /// </summary>
    public async Task LogOut()
    {
        using var tx = _connection.BeginTransaction();
        foreach (var found in AuthInfo.All(_connection.Db))
        {
            tx.Delete(found, false);
        }
        await tx.Commit();
    }

    public async Task<object> GetManifest(Build build)
    {
        using var result = await _client.GetAsync(build.Link);
        if (!result.IsSuccessStatusCode)
            throw new Exception($"Failed to get the manifest for {build.BuildId}. {result.StatusCode}");
        
        await using var responseStream = await result.Content.ReadAsStreamAsync();
        await using var deflateStream = new ZLibStream(responseStream, CompressionMode.Decompress);
        //var streamReader = (new StreamReader(deflateStream)).ReadToEnd();
        var buildDetails = JsonSerializer.Deserialize<BuildDetails>(deflateStream, _jsonSerializerOptions);

        var firstDepot = buildDetails!.Depots.First();

        var id = firstDepot.Manifest.ToString();
        using var depotResult = await _client.GetAsync(new Uri("https://cdn.gog.com/content-system/v2/meta/" + id[..2] + "/" + id[2..4] + "/" + id));
        if (!depotResult.IsSuccessStatusCode)
            throw new Exception($"Failed to get the depot for {build.BuildId}. {depotResult.StatusCode}");
        
        await using var depotStream = await depotResult.Content.ReadAsStreamAsync();
        await using var depotDeflateStream = new ZLibStream(depotStream, CompressionMode.Decompress);

        var depotAll = (new StreamReader(depotDeflateStream)).ReadToEnd();

        throw new NotImplementedException();
    }
}
