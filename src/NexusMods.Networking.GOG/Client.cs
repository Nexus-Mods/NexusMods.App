using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using Jitbit.Utils;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GOG;
using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.GOG.Values;
using NexusMods.Abstractions.Hashes;
using NexusMods.Abstractions.IO;
using NexusMods.Abstractions.IO.ChunkedStreams;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.Process;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.GOG.DTOs;
using NexusMods.Networking.GOG.Models;
using NexusMods.Paths;
using Polly;
using Polly.Retry;

namespace NexusMods.Networking.GOG;

internal class Client : IClient
{
    private readonly ILogger<Client> _logger;
    private readonly IConnection _connection;
    private readonly HttpClient _client;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly IOSInterop _osInterop;

    private static readonly Uri AuthorizationUri = new("https://auth.gog.com/auth");
    private static readonly Uri RedirectUrl = new("nxm://gog-auth");
    
    private const string ClientId = "58276512461627742";
    private const string ClientSecret = "211e87c957cb191888bdd333b794ee87757367883eb04f79653053302e291734";
    
    private static readonly Uri TokenUri = new("https://auth.gog.com/token");
    
    // A cache of secure URLs, which are CDN urls and associated auth data
    private readonly Dictionary<ProductId, SecureUrl[]> _secureUrls = new();
    
    // A lock for the secure URL cache
    private readonly SemaphoreSlim _secureUrlLock = new(1, 1);
    
    // A cache of blocks from the shared block cache. When a depot contains a small file container, we want to 
    // cache the blocks so that we don't have to re-download them if there are a lot of files that are found in the 
    // same block
    private readonly FastCache<Md5, Memory<byte>> _blockCache;
    
    /// <summary>
    /// TTL time for the secure URL cache.
    /// </summary>
    private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(5);
    
    /// <summary>
    /// A channel for incoming auth URLs.
    /// </summary>
    private Channel<NXMGogAuthUrl> _authUrls = Channel.CreateUnbounded<NXMGogAuthUrl>();

    internal readonly ResiliencePipeline _pipeline;

    /// <summary>
    /// Standard DI constructor.
    /// </summary>
    public Client(ILogger<Client> logger, IConnection connection, IOSInterop osInterop, HttpClient client, JsonSerializerOptions jsonSerializerOptions)
    {
        _osInterop = osInterop;
        _client = client;
        _logger = logger;
        _connection = connection;
        
        _jsonSerializerOptions = new JsonSerializerOptions(jsonSerializerOptions)
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        };

        _blockCache = new FastCache<Md5, Memory<byte>>();
        
        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }
    
    /// <summary>
    /// Getter for the HttpClient, used by chunked stream sources
    /// </summary>
    internal HttpClient HttpClient => _client;

    /// <summary>
    /// Start the login process.
    /// </summary>
    public async Task Login(CancellationToken token)
    {
        var authQuery = new Dictionary<string, string?>()
        {
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "response_type", "code" },
            { "redirect_uri", RedirectUrl.ToString() },
        };

        var urlTask = _authUrls.Reader.ReadAsync(token).AsTask();
        
        await _osInterop.OpenUrl(new Uri(QueryHelpers.AddQueryString(AuthorizationUri.ToString(), authQuery)), cancellationToken: token);
        
        var code = "";
        try
        {
            var result = await urlTask;
            code = result.Code;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogError(ex, "Failed to get the OAuth code.");
            return;
        }
        
        // Request the token
        var tokenQuery = new Dictionary<string, string?>
        {
            { "client_id", ClientId },
            { "client_secret", ClientSecret },
            { "code", code },
            { "grant_type", "authorization_code" },
            { "redirect_uri", RedirectUrl.ToString() },
        };

        var uri = new Uri(QueryHelpers.AddQueryString(TokenUri.ToString(), tokenQuery));

        var tokenResponse = await _client.GetFromJsonAsync<TokenResponse>(uri, token);

        if (tokenResponse == null)
        {
            _logger.LogError("The OAuth login request did not return a token.");
            return;
        }

        // Save the login information
        using var tx = _connection.BeginTransaction();
        var e = tx.TempId();
        if (TryGetAuthInfo(out var found))
            e = found.Id;

        tx.Add(e, AuthInfo.AccessToken, tokenResponse.AccessToken);
        tx.Add(e, AuthInfo.RefreshToken, tokenResponse.RefreshToken);
        tx.Add(e, AuthInfo.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
        tx.Add(e, AuthInfo.SessionId, tokenResponse.SessionId);
        tx.Add(e, AuthInfo.UserId, ulong.Parse(tokenResponse.UserId));
        await tx.Commit();
        _logger.LogInformation("Logged in successfully to GOG.");
    }
    

    /// <summary>
    /// Create a new HttpRequestMessage with the OAuth token.
    /// </summary>
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
    
    /// <summary>
    /// Returns true if the OAuth token needs to be refreshed.
    /// </summary>
    private bool NeedsRefresh(AuthInfo.ReadOnly authInfo)
    {
        return authInfo.ExpiresAt < DateTimeOffset.UtcNow + TimeSpan.FromMinutes(1);
    }

    /// <summary>
    /// Refresh the OAuth token.
    /// </summary>
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
            tx.Add(e, AuthInfo.ExpiresAt, DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn));
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
    
    /// <summary>
    /// Get all the builds for a given product and OS.
    /// </summary>
    public async Task<Build[]> GetBuilds(ProductId productId, OS os, CancellationToken token)
    {
        return await _pipeline.ExecuteAsync(async token =>
        {
            var msg = await CreateMessage(new Uri($"https://content-system.gog.com/products/{productId}/os/{os}/builds?generation=2"), CancellationToken.None);
            using var response = await _client.SendAsync(msg, token);
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return [];
                else
                    throw new Exception($"Failed to get builds for {productId} on {os}. {response.StatusCode}");
            }


            await using var responseStream = await response.Content.ReadAsStreamAsync(token);
            var content = await JsonSerializer.DeserializeAsync<BuildListResponse>(responseStream, _jsonSerializerOptions, token);

            if (content == null)
                throw new Exception("Failed to deserialize the builds response.");

            return content.Items;
        }, token);
    }

    /// <summary>
    /// Try to log out the user.
    /// </summary>
    public async Task LogOut()
    {
        using var tx = _connection.BeginTransaction();
        var infos = AuthInfo.All(_connection.Db).Select(ent => ent.Id).ToArray();
        foreach (var found in infos)
        {
            tx.Delete(found, false);
        }
        await tx.Commit();

        await _connection.Excise(infos);

    }
    
    /// <summary>
    /// Get the depot information for a build.
    /// </summary>
    public async Task<DepotInfo> GetDepot(Build build, CancellationToken token)
    {
        return await _pipeline.ExecuteAsync(async token =>
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
        );
    }

    /// <summary>
    /// Given a depot, a build, and a path, return a stream to the file.
    /// </summary>
    public async Task<Stream> GetFileStream(Build build, DepotInfo depotInfo, RelativePath path, CancellationToken token)
    { 
        return await _pipeline.ExecuteAsync(async token =>
            {
                var itemInfo = depotInfo.Items.FirstOrDefault(f => f.Path == path);
                if (itemInfo == null)
                    throw new KeyNotFoundException($"The path {path} was not found in the depot.");

                var secureUrl = await GetSecureUrl(build.ProductId, token);

                if (itemInfo.SfcRef == null)
                {
                    // No small file container, just return a stream to the chunks
                    var size = Size.FromLong(itemInfo.Chunks.Sum(c => (long)c.Size.Value));
                    var source = new ChunkedStreamSource(this, itemInfo.Chunks, size,
                        secureUrl
                    );
                    return (Stream)new ChunkedStream<ChunkedStreamSource>(source);
                }
                else
                {
                    // If the file is in a small file container, we need a stream to the outer container, and then a substream to the inner file
                    var subSize = Size.FromLong(depotInfo.SmallFilesContainer!.Chunks.Sum(c => (long)c.Size.Value));
                    var sfcSource = new ChunkedStreamSource(this, depotInfo.SmallFilesContainer!.Chunks, subSize,
                        secureUrl, putInCache: true);
                    var sfcStream = new ChunkedStream<ChunkedStreamSource>(sfcSource);
                    var subStream = new SubStream(sfcStream, itemInfo.SfcRef.Offset, itemInfo.SfcRef.Size);
                    return subStream;
                }
            }, token);
    }


    /// <summary>
    /// Returns a (possibly cached) secure URL for a product.
    /// </summary>
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

    /// <summary>
    /// Try to get a cached block.
    /// </summary>
    internal bool TryGetCachedBlock(Md5 md5, out Memory<byte> found)
    {
        return _blockCache.TryGet(md5, out found);
    }

    /// <summary>
    /// Add a block to the global cache.
    /// </summary>
    public void AddCachedBlock(Md5 md5, Memory<byte> buffer)
    {
        _blockCache.AddOrUpdate(md5, buffer, CacheTime);
    }

    /// <summary>
    /// Feed a new auth URL into the client.
    /// </summary>
    /// <param name="url"></param>
    public void AuthUrl(NXMGogAuthUrl url)
    {
        _authUrls.Writer.TryWrite(url);
    }
}
