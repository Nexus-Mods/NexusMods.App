using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Jitbit.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.ChunkedStreams;
using NexusMods.Abstractions.OAuth;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.GOG.DTOs;
using NexusMods.Networking.GOG.Models;
using NexusMods.Paths;

namespace NexusMods.Networking.GOG;

internal class Client
{
    private readonly IOAuthUserInterventionHandler _oAuthUserInterventionHandler;
    private readonly ILogger<Client> _logger;
    private readonly IConnection _connection;
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private const string ClientId = "46899977096215655";
    private const string ClientSecret = "9d85c43b1482497dbbce61f6e4aa173a433796eeae2ca8c5f6129f2dc4de46d9";

    private static readonly Uri AuthorizationUri =
        new("https://auth.gog.com/auth?client_id=46899977096215655&redirect_uri=https://embed.gog.com/on_login_success?origin=client&response_type=code&layout=galaxy");

    private static readonly Uri CallbackPrefix = new("https://embed.gog.com/on_login_success");
    
    private static readonly Uri TokenUri = new("https://auth.gog.com/token");
    
    private readonly Dictionary<ProductId, SecureUrl[]> _secureUrls = new();
    
    private readonly SemaphoreSlim _secureUrlLock = new(1, 1);
    
    private readonly FastCache<Md5, Memory<byte>> _blockCache;
    private static readonly TimeSpan CacheTime = TimeSpan.FromSeconds(5);

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

        _blockCache = new FastCache<Md5, Memory<byte>>();
    }
    
    /// <summary>
    /// Getter for the HttpClient, used by chunked stream sources
    /// </summary>
    internal HttpClient HttpClient => _client;


    public async Task Login(CancellationToken token)
    {
        var request = await _oAuthUserInterventionHandler.HandleOAuthRequest(new OAuthLoginRequest
            {
                AuthorizationUrl = AuthorizationUri,
                CallbackType = CallbackType.Capture,
                CallbackPrefix = CallbackPrefix,
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
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", "https://embed.gog.com/on_login_success?origin=client" },
        };
        
        var uri = new Uri(QueryHelpers.AddQueryString(TokenUri.ToString(), tokenQuery));
        
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
        await _semaphore.WaitAsync(token);
        try
        {
            if (!TryGetAuthInfo(out var authInfo))
                throw new InvalidOperationException("No authentication information found.");

            if (!NeedsRefresh(authInfo))
                return authInfo;

            var tokenQuery = new Dictionary<string, string?>
            {
                { "client_id", ClientId },
                { "client_secret", ClientSecret },
                { "grant_type", "refresh_token" },
                { "refresh_token", authInfo.RefreshToken },
            };
        
            var uri = new Uri(QueryHelpers.AddQueryString(TokenUri.ToString(), tokenQuery));
        
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
    
    public async Task<DepotInfo> GetDepot(Build build, CancellationToken token)
    {
        using var result = await _client.GetAsync(build.Link, token);
        if (!result.IsSuccessStatusCode)
            throw new Exception($"Failed to get the manifest for {build.BuildId}. {result.StatusCode}");
        
        await using var responseStream = await result.Content.ReadAsStreamAsync(token);
        await using var deflateStream = new ZLibStream(responseStream, CompressionMode.Decompress);
        //var streamReader = (new StreamReader(deflateStream)).ReadToEnd();
        var buildDetails = JsonSerializer.Deserialize<BuildDetails>(deflateStream, _jsonSerializerOptions);

        var firstDepot = buildDetails!.Depots.First();

        var id = firstDepot.Manifest.ToString();
        using var depotResult = await _client.GetAsync(new Uri("https://cdn.gog.com/content-system/v2/meta/" + id[..2] + "/" + id[2..4] + "/" + id), token);
        if (!depotResult.IsSuccessStatusCode)
            throw new Exception($"Failed to get the depot for {build.BuildId}. {depotResult.StatusCode}");
        
        await using var depotStream = await depotResult.Content.ReadAsStreamAsync(token);
        await using var depotDeflateStream = new ZLibStream(depotStream, CompressionMode.Decompress);

        var depot = await JsonSerializer.DeserializeAsync<DepotResponse>(depotDeflateStream, _jsonSerializerOptions, token);
        return depot!.Depot;
    }

    public async Task<Stream> GetFileStream(Build build, DepotInfo depotInfo, RelativePath path, CancellationToken token)
    {
        var itemInfo = depotInfo.Items.FirstOrDefault(f => f.Path == path);
        if (itemInfo == null)
            throw new KeyNotFoundException($"The path {path} was not found in the depot.");
        
        var secureUrl = await GetSecureUrl(build.ProductId, token);

        if (itemInfo.SfcRef == null)
        {
            var size = Size.FromLong(itemInfo.Chunks.Sum(c => (long)c.Size.Value));
            var source = new ChunkedStreamSource(this, itemInfo.Chunks, size, secureUrl);
            return new ChunkedStream<ChunkedStreamSource>(source);
        }
        else
        {
            var subSize = Size.FromLong(depotInfo.SmallFilesContainer!.Chunks.Sum(c => (long)c.Size.Value));
            var sfcSource = new ChunkedStreamSource(this, depotInfo.SmallFilesContainer!.Chunks, subSize, secureUrl);
            var sfcStream = new ChunkedStream<ChunkedStreamSource>(sfcSource);
            var subStream = new SubStream(sfcStream, itemInfo.SfcRef.Offset, itemInfo.SfcRef.Size);
            return subStream;
        }
    }


    private async Task<SecureUrl> GetSecureUrl(ProductId productId, CancellationToken token)
    {
        try
        {
            await _secureUrlLock.WaitAsync(token);
            if (_secureUrls.TryGetValue(productId, out var found))
            {
                // Later we can make this more advanced, but for now just return the first one
                var server = found.FirstOrDefault();
                if (server != null)
                {
                    var expiresAt = server.Parameters.ExpiresAt;
                    if (expiresAt == null)
                        return server;
                    if (expiresAt > DateTimeOffset.UtcNow + TimeSpan.FromHours(3))
                        return server;
                }
            }

            var request = await CreateMessage(new Uri($"https://content-system.gog.com/products/{productId.Value}/secure_link?generation=2&_version=2&path=/"), token);

            using var response = await _client.SendAsync(request, token);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to get the secure URLs for {productId}. {response.StatusCode}");

            await using var responseStream = await response.Content.ReadAsStreamAsync(token);
            var urls = await JsonSerializer.DeserializeAsync<SecureUrlResponse>(responseStream, cancellationToken: token);
            if (urls == null)
                throw new Exception("Failed to deserialize the secure URLs response.");

            var sorted = urls.Urls.OrderBy(s => s.Priority).ToArray();
            _secureUrls[productId] = sorted;
            return sorted.First();
        }
        finally
        {
            _secureUrlLock.Release();
        }
    }

    internal bool TryGetCachedBlock(Md5 md5, out Memory<byte> o)
    {
        return _blockCache.TryGet(md5, out o);
    }

    public void AddCachedBlock(Md5 md5, Memory<byte> buffer)
    {
        _blockCache.AddOrUpdate(md5, buffer, CacheTime);
    }
}
