using System.Reactive.Concurrency;
using System.Reactive.Linq;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.Serialization;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Extensions.BCL;
using NexusMods.Networking.NexusWebApi.Auth;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Component for handling login and logout from the Nexus Mods
/// </summary>
[PublicAPI]
public sealed class LoginManager : IDisposable, ILoginManager
{
    private readonly ILogger<LoginManager> _logger;
    private readonly OAuth _oauth;
    private readonly IDataStore _dataStore;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly NexusApiClient _nexusApiClient;
    private readonly IAuthenticatingMessageFactory _msgFactory;

    /// <summary>
    /// Allows you to subscribe to notifications of when the user information changes.
    /// </summary>
    public IObservable<UserInfo?> UserInfo { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    public IObservable<bool> IsLoggedIn => UserInfo.Select(info => info is not null);

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    public IObservable<bool> IsPremium => UserInfo.Select(info => info?.IsPremium ?? false);

    /// <summary>
    /// The user's avatar
    /// </summary>
    public IObservable<Uri?> Avatar => UserInfo.Select(info => info?.AvatarUrl);

    /// <summary>
    /// Constructor.
    /// </summary>
    public LoginManager(
        NexusApiClient nexusApiClient,
        IAuthenticatingMessageFactory msgFactory,
        OAuth oauth,
        IDataStore dataStore,
        IProtocolRegistration protocolRegistration,
        ILogger<LoginManager> logger)
    {
        _oauth = oauth;
        _msgFactory = msgFactory;
        _nexusApiClient = nexusApiClient;
        _dataStore = dataStore;
        _protocolRegistration = protocolRegistration;
        _logger = logger;

        UserInfo = _dataStore.IdChanges
            // NOTE(err120): Since IDs don't change on startup, we can insert
            // a fake change at the start of the observable chain. This will only
            // run once at startup and notify the subscribers.
            .Merge(Observable.Return(JWTTokenEntity.StoreId))
            .Where(id => id.Equals(JWTTokenEntity.StoreId))
            .ObserveOn(TaskPoolScheduler.Default)
            .SelectMany(async _ => await Verify(CancellationToken.None));
    }

    private CachedObject<UserInfo> _cachedUserInfo = new(TimeSpan.FromHours(1));
    private readonly SemaphoreSlim _verifySemaphore = new(initialCount: 1, maxCount: 1);

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

        var jwtToken = await _oauth.AuthorizeRequest(token);
        var newTokenEntity = JWTTokenEntity.From(jwtToken);
        if (newTokenEntity is null)
        {
            _logger.LogError("Invalid new token!");
            return;
        }

        _dataStore.Put(JWTTokenEntity.StoreId, newTokenEntity);
    }

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    public Task Logout()
    {
        _dataStore.Delete(JWTTokenEntity.StoreId);
        _cachedUserInfo.Evict();
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _verifySemaphore.Dispose();
    }
}
