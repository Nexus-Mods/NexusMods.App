using System.Reactive.Linq;
using Avalonia.Input.Raw;
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

    private readonly IDisposable _observeDatomDisposable;

    /// <inheritdoc />
    public bool IsPremium { get; private set; }

    /// <inheritdoc />
    public bool IsOAuthLogin => JWTToken.All(_conn.Db).Any();

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
                _cachedUserInfo.Evict();

                if (!hasValue)
                {
                    _userInfo.OnNext(value: null);
                    IsPremium = false;
                }
                else
                {
                    var userInfo = await Verify(cancellationToken);
                    _userInfo.OnNext(userInfo);
                    IsPremium = userInfo?.IsPremium ?? false;
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
        await _conn.Excise(JWTToken.All(_conn.Db).Select(e => e.Id).ToArray());
    }
    
    
    /// <inheritdoc/>
    public void Dispose()
    {
        _verifySemaphore.Dispose();
        _observeDatomDisposable.Dispose();
    }
}
