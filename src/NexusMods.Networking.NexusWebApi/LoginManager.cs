using DynamicData.Aggregation;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.NexusWebApi;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.CrossPlatform.ProtocolRegistration;
using NexusMods.Extensions.BCL;
using NexusMods.MnemonicDB.Abstractions;
using NexusMods.MnemonicDB.Abstractions.Query;
using NexusMods.MnemonicDB.Abstractions.TxFunctions;
using NexusMods.Networking.NexusWebApi.Auth;
using R3;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Component for handling login and logout from the Nexus Mods
/// </summary>
[PublicAPI]
public sealed class LoginManager : IDisposable, ILoginManager
{
    private readonly ILogger<LoginManager> _logger;
    private readonly OAuth _oauth;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly NexusApiClient _nexusApiClient;
    private readonly IAuthenticatingMessageFactory _msgFactory;

    private readonly BehaviorSubject<UserInfo?> _userInfo = new(initialValue: null);

    /// <inheritdoc/>
    public Observable<UserInfo?> UserInfoObservable => _userInfo;

    /// <inheritdoc/>
    public UserInfo? UserInfo => _cachedUserInfo.Get();

    private readonly IDisposable _observeDatomDisposable;

    /// <summary>
    /// Constructor.
    /// </summary>
    public LoginManager(
        IConnection conn,
        NexusApiClient nexusApiClient,
        IAuthenticatingMessageFactory msgFactory,
        OAuth oauth,
        IProtocolRegistration protocolRegistration,
        ILogger<LoginManager> logger)
    {
        _oauth = oauth;
        _conn = conn;
        _msgFactory = msgFactory;
        _nexusApiClient = nexusApiClient;
        _protocolRegistration = protocolRegistration;
        _logger = logger;

        _observeDatomDisposable = _conn
            .ObserveDatoms(JWTToken.PrimaryAttribute)
            .IsNotEmpty()
            .ToObservable()
            .DistinctUntilChanged()
            .SubscribeAwait(async (hasValue, cancellationToken) =>
            {
                if (!hasValue)
                {
                    _cachedUserInfo.Evict();
                    _userInfo.OnNext(value: null);
                }
                else
                {
                    var userInfo = await Verify(cancellationToken);
                    _cachedUserInfo.Store(userInfo);
                    _userInfo.OnNext(userInfo);
                }
            }, awaitOperation: AwaitOperation.Sequential, configureAwait: false);
    }

    private CachedObject<UserInfo> _cachedUserInfo = new(TimeSpan.FromHours(1));
    private readonly SemaphoreSlim _verifySemaphore = new(initialCount: 1, maxCount: 1);
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

    /// <inheritdoc />
    public async Task<UserInfo?> GetUserInfoAsync(CancellationToken token)
    {
        return await Verify(token);
    }

    /// <inheritdoc />
    public async Task<bool> GetIsUserLoggedInAsync(CancellationToken token = default)
    {
        return await GetUserInfoAsync(token) is not null;
    }

    /// <summary>
    /// Show a browser and log into Nexus Mods
    /// </summary>
    /// <param name="token"></param>
    public async Task LoginAsync(CancellationToken token = default)
    {
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

        var newTokenEntity = JWTToken.Create(_conn.Db, tx, jwtToken);
        if (!newTokenEntity.HasValue)
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
        var tokenEntities = JWTToken.All(_conn.Db).Select(e => e.Id).ToArray();

        // Retract the entities first, so the UI updates, then excise them
        using var tx = _conn.BeginTransaction();
        foreach (var entity in tokenEntities)
            tx.Delete(entity, false);
        await tx.Commit();


        await _conn.Excise(tokenEntities);
    }

    /// <inheritdoc />
    public async Task<bool> EnsureLoggedIn(string message, CancellationToken token = default)
    {
        if (await GetIsUserLoggedInAsync(token)) return true;
        
        // TODO: Improve this to show a special dialog to inform the user they need to log in to perform the operation
        // https://github.com/Nexus-Mods/NexusMods.App/issues/2562
        _logger.LogWarning("This operation requires login: {Message}", message);
        await LoginAsync(token);
        return await GetIsUserLoggedInAsync(token);
    }
    
    
    /// <inheritdoc/>
    public void Dispose()
    {
        _verifySemaphore.Dispose();
        _observeDatomDisposable.Dispose();
    }
}
