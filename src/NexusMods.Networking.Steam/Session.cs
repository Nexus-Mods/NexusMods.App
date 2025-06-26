using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.GameLocators.Stores.Steam;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Models;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.Steam.DTOs;
using NexusMods.Paths;
using NexusMods.Sdk.IO;
using Polly;
using Polly.Retry;
using SteamKit2;
using SteamKit2.Authentication;
using SteamKit2.CDN;

namespace NexusMods.Networking.Steam;

/// <summary>
/// Base class for a Steam session. Steam works in a rather strange way. Internally it communicates
/// via websockets. This means that many of the operations require sending a message, then listening
/// on a separate callback handler for the response. This class attempts to abstract this process and
/// provide a cleaner interface for other parts of the app.
/// </summary>
public class Session : ISteamSession
{
    private bool _isConnected = false;
    private bool _isLoggedOn = false;

    internal readonly ILogger<Session> _logger;
    private readonly IAuthInterventionHandler _handler;

    /// <summary>
    /// Base steam component, this is used for communicating with the Steam network.
    /// </summary>
    private readonly SteamClient _steamClient;
    
    /// <summary>
    /// The component used for user related operations.
    /// </summary>
    private readonly SteamUser _steamUser;
    
    /// <summary>
    /// The component used for getting information about apps (games)
    /// </summary>
    private readonly SteamApps _steamApps;
    
    /// <summary>
    /// Component for getting content information (actual game data)
    /// </summary>
    private readonly SteamContent _steamContent;
    
    /// <summary>
    /// CDN data, used to get download locations for game data.
    /// </summary>
    private readonly Client _cdnClient;

    private readonly CallbackManager _callbacks;
    private readonly IAuthStorage _authStorage;
    private readonly CDNPool _cdnPool;

    private ConcurrentDictionary<(AppId, DepotId), byte[]> _depotKeys = new();
    private ConcurrentDictionary<(AppId, DepotId, ManifestId, string Branch), ulong> _manifestRequestCodes = new();
    internal readonly ResiliencePipeline _pipeline;
    private readonly IConnection _connection;
    private readonly ISteamGame[] _steamGames;

    public Session(ILogger<Session> logger, IAuthInterventionHandler handler, IAuthStorage storage, IConnection connection, IEnumerable<ILocatableGame> games)
    {
        _logger = logger;
        _connection = connection;
        _handler = handler;
        _authStorage = storage;
        _steamGames = games.OfType<ISteamGame>().ToArray();


        var steamConfiguration = SteamConfiguration.Create(configurator =>
        {
            // The client will dispose of these on its own
            configurator.WithHttpClientFactory(() =>
                {
                    var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.Add(
                        new System.Net.Http.Headers.ProductInfoHeaderValue("NexusMods.Networking.Steam", "1.0")
                    );
                    return client;
                }
            );
        });
        _steamClient = new SteamClient(steamConfiguration);
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _steamApps = _steamClient.GetHandler<SteamApps>()!;
        _steamContent = _steamClient.GetHandler<SteamContent>()!;
        _cdnClient = new Client(_steamClient);
        _cdnPool = new CDNPool(this);
        
        // Some parts of this interface use callbacks instead of more natural async methods. So we need to register
        // those callbacks here.
        _callbacks = new CallbackManager(_steamClient);
        _callbacks.Subscribe(WrapAsync<SteamClient.ConnectedCallback>(ConnectedCallback));
        _callbacks.Subscribe(WrapAsync<SteamClient.DisconnectedCallback>(DisconnectedCallback));
        _callbacks.Subscribe(WrapAsync<SteamUser.LoggedOnCallback>(LoggedOnCallback));
        _callbacks.Subscribe(WrapAsync<SteamApps.LicenseListCallback>(LicenseListCallback));

        _pipeline = new ResiliencePipelineBuilder()
            .AddRetry(new RetryStrategyOptions())
            .AddTimeout(TimeSpan.FromSeconds(10))
            .Build();
    }

    /// <summary>
    /// The Steam Content module
    /// </summary>
    internal SteamContent Content => _steamContent;
    
    /// <summary>
    /// The CDN client, used for downloading game data.
    /// </summary>
    internal Client CDNClient => _cdnClient;
    
    /// <summary>
    /// The CDN pool, used for downloading game data.
    /// </summary>
    internal CDNPool CDNPool => _cdnPool;

    private async Task LicenseListCallback(SteamApps.LicenseListCallback arg)
    {
        _logger.LogInformation("Got {LicenseCount} licenses from Steam", arg.LicenseList.Count);
        var appInfos = await GetAppIdsForPackages(arg.LicenseList.Select(r => r.PackageID));

        var licenses = (from info in appInfos
            from package in info.Packages.Values
            from keyvalue in package.KeyValues.Children
            where keyvalue.Name == "appids"
            from appId in keyvalue.Children
            let parsedAppId = AppId.From(uint.Parse(appId.Value ?? "0"))
            group parsedAppId by PackageId.From(package.ID)
            into grouped
            select grouped)
            .ToArray();
        
        _logger.LogInformation("Got details {LicenseCount} licenses from Steam, caching the data", licenses.Length);
        
        var db = _connection.Db;
        using var tx = _connection.BeginTransaction();
        bool changes = false;
        foreach (var grouping in licenses)
        {
            var existing = SteamLicenses.FindByPackageId(db, grouping.Key).FirstOrDefault();
            if (existing.IsValid())
            {
                foreach (var appId in grouping)
                {
                    if (!existing.AppIds.Contains(appId))
                    {
                        tx.Add(existing.Id, SteamLicenses.AppIds, appId);
                        changes = true;
                    }
                }
                foreach (var appId in existing.AppIds)
                {
                    if (!grouping.Contains(appId))
                    {
                        tx.Retract(existing.Id, SteamLicenses.AppIds, appId);
                        changes = true;
                    }
                }
            }
            else
            {
                _ = new SteamLicenses.New(tx)
                {
                    PackageId = grouping.Key,
                    AppIds = grouping.ToList(),
                };
                changes = true;
            }
        }

        if (changes) 
            await tx.Commit();
    }
    
    private async Task LoggedOnCallback(SteamUser.LoggedOnCallback callback)
    {
        if (callback.Result != EResult.OK)
        {
            _logger.LogError("Failed to log on to Steam network: {Result}", callback.Result);
            _isLoggedOn = false;
            return;
        }
        
        _isLoggedOn = true;
        _logger.LogInformation("Logged on to Steam network.");
        
        return;

    }

    private async Task DisconnectedCallback(SteamClient.DisconnectedCallback callback)
    {
        _logger.LogInformation("Disconnected from Steam network.");
    }
    
    private async Task ConnectedCallback(SteamClient.ConnectedCallback callback)
    {
        _isConnected = true;
        var (success, data) = await _authStorage.TryLoad();
        if (success)
        {
            _logger.LogInformation("Using saved auth data to log in.");
            var authData = AuthData.Load(data);
            
            _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = authData.Username,
                    AccessToken = authData.RefreshToken,
                }
            );
        }
        else if (Environment.GetEnvironmentVariable("STEAM_USER") != null)
        {
            _logger.LogInformation("Using environment variables to log in.");
            var authSession = await _steamClient.Authentication.BeginAuthSessionViaCredentialsAsync(new AuthSessionDetails
            {
                Username = Environment.GetEnvironmentVariable("STEAM_USER"),
                Password = Environment.GetEnvironmentVariable("STEAM_PASS"),
            });
            
            var pollResponse = await authSession.PollingWaitForResultAsync();
            await _authStorage.SaveAsync(new AuthData
            {
                Username = pollResponse.AccountName,
                RefreshToken = pollResponse.RefreshToken,
            }.Save());
            
            _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                }
            );
        }
        else
        {
            _logger.LogInformation("No saved auth data, logging in via QR code.");
            var authSession = await _steamClient.Authentication.BeginAuthSessionViaQRAsync(new AuthSessionDetails());

            authSession.ChallengeURLChanged = () => { _handler.ShowQRCode(new Uri(authSession.ChallengeURL, UriKind.Absolute), CancellationToken.None); };

            _handler.ShowQRCode(new Uri(authSession.ChallengeURL, UriKind.Absolute), CancellationToken.None);
            var pollResponse = await authSession.PollingWaitForResultAsync();

            await _authStorage.SaveAsync(new AuthData
            {
                Username = pollResponse.AccountName,
                RefreshToken = pollResponse.RefreshToken,
            }.Save());
            
            _steamUser.LogOn(new SteamUser.LogOnDetails
                {
                    Username = pollResponse.AccountName,
                    AccessToken = pollResponse.RefreshToken,
                }
            );
        }
    }

    /// <summary>
    /// Wraps an async action in a synchronous action.
    /// </summary>
    private Action<T> WrapAsync<T>(Func<T, Task> action)
    {
        return arg => Task.Run(async () => await action(arg));
    }

    /// <inheritdoc />
    public async Task Connect(CancellationToken token)
    {
        await ConnectedAsync(token);
    }

    public async Task<ProductInfo> GetProductInfoAsync(AppId appId, CancellationToken cancellationToken = default)
    {
        await ConnectedAsync(cancellationToken);

        var jobs = await _steamApps.PICSGetProductInfo(new SteamApps.PICSRequest(appId.Value), null);
        if (jobs.Failed) throw new Exception($"Failed to get product info for app `{appId}`");

        var results = jobs.Results;
        if (results is null || results.Count == 0) throw new Exception($"Found no product info for app `{appId}`");

        return ProductInfoParser.Parse(results[0]);
    }

    private async Task<SteamApps.PICSProductInfoCallback[]> GetAppIdsForPackages(IEnumerable<uint> packageIds, CancellationToken cancellationToken = default)
    {
        await ConnectedAsync(cancellationToken);

        var jobs = await _steamApps.PICSGetProductInfo([], packageIds.Select(id => new SteamApps.PICSRequest(id)));
        
        if (jobs.Failed) 
            throw new Exception("Failed to get app ids for packages");

        return jobs.Results!.ToArray();
    }

    /// <summary>
    /// Performs a login if required, returns once the login is complete
    /// </summary>
    private async Task ConnectedAsync(CancellationToken cancellationToken)
    {
        if (!_isConnected)
        {
            _steamClient.Connect();
        }
        
        while (!_isLoggedOn)
        {
            await _callbacks.RunWaitCallbackAsync(cancellationToken);
        }
    }

    public async Task<ulong> GetManifestRequestCodeAsync(AppId appId, DepotId depotId, ManifestId manifestId, string branch)
    {
        if (_manifestRequestCodes.TryGetValue((appId, depotId, manifestId, branch), out var found))
            return found;
        await ConnectedAsync(CancellationToken.None);
        
        var requestCodeResult = await _steamContent.GetManifestRequestCode(depotId.Value, appId.Value, manifestId.Value, branch);
        if (requestCodeResult == 0)
        {
            _logger.LogWarning("Failed to get request code for depot {DepotId} manifest {ManifestId}", depotId.Value, manifestId.Value);
            throw new Exception("Failed to get request code for depot " + depotId.Value + " manifest " + manifestId.Value);
        }

        _logger.LogInformation("Got request code depot {DepotId} manifest {ManifestId}", depotId.Value, manifestId.Value);

        _manifestRequestCodes.TryAdd((appId, depotId, manifestId, branch), requestCodeResult);
        return requestCodeResult;
    }
    
    /// <summary>
    /// Get a depot decryption key for a given depot.
    /// </summary>
    public async Task<byte[]> GetDepotKey(AppId appId, DepotId depotId)
    {
        // Try to get the cached key first
        
        if (_depotKeys.TryGetValue((appId, depotId), out var keyBytes))
            return keyBytes;
        await ConnectedAsync(CancellationToken.None);
        
        var key = await _steamApps.GetDepotDecryptionKey(depotId.Value, appId.Value);
        if (key.Result != EResult.OK)
        {
            _logger.LogWarning("Failed to get depot key for depot `{DepotId}`", depotId.Value);
            throw new Exception($"Failed to get depot key for depot `{depotId.Value}`");
        }

        _logger.LogInformation("Got depot key for depot `{DepotId}`", depotId.Value);
        _depotKeys.TryAdd((appId, depotId), key.DepotKey);
        
        return key.DepotKey;
    }

    public async Task<Manifest> GetManifestContents(AppId appId, DepotId depotId, ManifestId manifestId, string branch, CancellationToken token = default)
    {
        await ConnectedAsync(token);
        return await _pipeline.ExecuteAsync(async token => await _cdnPool.GetManifestContents(appId, depotId, manifestId,
                branch, token
            ), token
        );
    }

    public Stream GetFileStream(AppId appId, Manifest manifest, RelativePath file)
    {
        var chunkedProvider = new DepotChunkProvider(this, appId, manifest.DepotId,
            manifest, file
        );
        // 48 1MB chunks, 32 preloaded
        return new ChunkedStream<DepotChunkProvider>(chunkedProvider, capacity: 48, preFetch: 32);
    }
}
