using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Steam;
using NexusMods.Abstractions.Steam.DTOs;
using NexusMods.Abstractions.Steam.Values;
using NexusMods.Networking.Steam.DTOs;
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
    
    private readonly ILogger<Session> _logger;
    private readonly IAuthInterventionHandler _handler;
    private readonly SteamConfiguration _steamConfiguration;
    
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

    public Session(ILogger<Session> logger, IAuthInterventionHandler handler, IAuthStorage storage, HttpClient httpClient)
    {
        
        _logger = logger;
        _handler = handler;
        _authStorage = storage;

        _steamConfiguration = SteamConfiguration.Create(configurator =>
        {
            configurator.WithHttpClientFactory(() => httpClient);
        });
        _steamClient = new SteamClient(_steamConfiguration);
        _steamUser = _steamClient.GetHandler<SteamUser>()!;
        _steamApps = _steamClient.GetHandler<SteamApps>()!;
        _steamContent = _steamClient.GetHandler<SteamContent>()!;
        _cdnClient = new Client(_steamClient);
        
        
        _callbacks = new CallbackManager(_steamClient);
        _callbacks.Subscribe(WrapAsync<SteamClient.ConnectedCallback>(ConnectedCallback));
        _callbacks.Subscribe(WrapAsync<SteamClient.DisconnectedCallback>(DisconnectedCallback));
        _callbacks.Subscribe(WrapAsync<SteamUser.LoggedOnCallback>(LoggedOnCallback));
        _callbacks.Subscribe(WrapAsync<SteamApps.LicenseListCallback>(LicenseListCallback));
    }

    private Task LicenseListCallback(SteamApps.LicenseListCallback arg)
    {
        return Task.CompletedTask;
    }

    private async Task PICSProductInfoCallback(SteamApps.PICSProductInfoCallback callback)
    {
        _logger.LogInformation("Received PICSProductInfoCallback");
    }

    private async Task LoggedOnCallback(SteamUser.LoggedOnCallback callback)
    {
        _isLoggedOn = true;
        _logger.LogInformation("Logged on to Steam network.");
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


    public async Task<ProductInfo> GetProductInfoAsync(AppId appId, CancellationToken cancellationToken = default)
    {
        await ConnectedAsync(cancellationToken);
        var jobs = await _steamApps.PICSGetProductInfo(new SteamApps.PICSRequest(appId.Value), null);
        if (jobs.Failed)
            throw new Exception("Failed to get product info for app " + appId.Value);

        return ProductInfoParser.Parse(jobs.Results![0]);
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
}
