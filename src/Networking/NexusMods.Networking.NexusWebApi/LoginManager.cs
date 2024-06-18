using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using JetBrains.Annotations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.MnemonicDB.Attributes;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Component for handling login and logout from the Nexus Mods
/// </summary>
[PublicAPI]
public sealed class LoginManager : IDisposable, ILoginManager, IHostedService
{
    private readonly ILogger<LoginManager> _logger;
    private readonly OAuth _oauth;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly NexusApiClient _nexusApiClient;
    private readonly IAuthenticatingMessageFactory _msgFactory;
    private Task? _startupTask = null;

    private CompositeDisposable _subscriptions = new CompositeDisposable();


    /// <summary>
    /// Allows you to subscribe to notifications of when the user information changes.
    /// </summary>
    public IObservable<UserInfo?> UserInfoObservable { get; }
    
    /// <summary>
    /// The current user information
    /// </summary>
    public UserInfo? UserInfo { get; private set; }
    
    /// <summary>
    /// True if the user is logged in
    /// </summary>
    public IObservable<bool> IsLoggedInObservable => UserInfoObservable.Select(info => info is not null);
    
    /// <summary>
    /// True if the user is logged in
    /// </summary>
    public bool IsLoggedIn => UserInfo is not null;

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    public IObservable<bool> IsPremiumObservable => UserInfoObservable.Select(info => info?.IsPremium ?? false);

    /// <summary>
    /// The user's avatar
    /// </summary>
    public IObservable<Uri?> AvatarObservable => UserInfoObservable.Select(info => info?.AvatarUrl);

    /// <summary>
    /// Constructor.
    /// </summary>
    public LoginManager(
        IConnection conn,
        NexusApiClient nexusApiClient,
        IAuthenticatingMessageFactory msgFactory,
        OAuth oauth,
        IRepository<JWTToken.Model> jwtTokenRepository,
        IProtocolRegistration protocolRegistration,
        ILogger<LoginManager> logger)
    {
        _oauth = oauth;
        _conn = conn;
        _msgFactory = msgFactory;
        _nexusApiClient = nexusApiClient;
        _jwtTokenRepository = jwtTokenRepository;
        _protocolRegistration = protocolRegistration;
        _logger = logger;

        UserInfoObservable = _jwtTokenRepository.Observable
            .ToObservableChangeSet()
            // NOTE(err120): Since IDs don't change on startup, we can insert
            // a fake change at the start of the observable chain. This will only
            // run once at startup and notify the subscribers.
            .ObserveOn(TaskPoolScheduler.Default)
            .SelectMany(async _ => await Verify(CancellationToken.None));

        _subscriptions.Add(UserInfoObservable.Subscribe(userInfo => UserInfo = userInfo));
        
    }

    private CachedObject<UserInfo> _cachedUserInfo = new(TimeSpan.FromHours(1));
    private readonly SemaphoreSlim _verifySemaphore = new(initialCount: 1, maxCount: 1);
    private readonly IRepository<JWTToken.Model> _jwtTokenRepository;
    private readonly IConnection _conn;

    private async Task<UserInfo?> Verify(CancellationToken cancellationToken)
    {
        var cachedValue = _cachedUserInfo.Get();
        if (cachedValue is not null) return cachedValue;

        using var waiter = _verifySemaphore.WaitDisposable(cancellationToken);
        cachedValue = _cachedUserInfo.Get();
        if (cachedValue is not null) return cachedValue;

        var isAuthenticated = await _msgFactory.IsAuthenticated();
        if (!isAuthenticated) return null;

        var userInfo = await _msgFactory.Verify(_nexusApiClient, cancellationToken);
        _cachedUserInfo.Store(userInfo);

        return userInfo;
    }

    /// <summary>
    /// Show a browser and log into Nexus Mods
    /// </summary>
    /// <param name="token"></param>
    public async Task LoginAsync(CancellationToken token = default)
    {
        // temporary but if we want oauth to work we _have_ to be registered as the nxm handler
        await _protocolRegistration.RegisterSelf("nxm");

        JwtTokenReply? jwtToken;
        try
        {
            jwtToken = await _oauth.AuthorizeRequest(token);
        }
        catch (TaskCanceledException e)
        {
            _logger.LogError(e, "Unable to login: task was canceled");
            return;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while logging in");
            return;
        }
        
        if (jwtToken is null)
        {
            _logger.LogError("Invalid new token in Login Manager");
            return;
        }
        
        using var tx = _conn.BeginTransaction();

        var newTokenEntity = JWTToken.Model.Create(_conn.Db, tx, jwtToken!);
        if (newTokenEntity is null)
        {
            _logger.LogError("Invalid new token data");
            return;
        }

        await tx.Commit();
    }

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    public async Task Logout()
    {
        _cachedUserInfo.Evict();
        await _jwtTokenRepository.Delete(_jwtTokenRepository.All.First());
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _verifySemaphore.Dispose();
        _subscriptions.Dispose();
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (this)
        {
            _startupTask ??= Startup(cancellationToken);
        }
        await _startupTask;
    }
    
    private async Task Startup(CancellationToken cancellationToken)
    {
        await ((IHostedService)_jwtTokenRepository).StartAsync(cancellationToken);
        var userInfo = await Verify(cancellationToken);
        if (userInfo is not null)
        {
            UserInfo = userInfo;
        }
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
