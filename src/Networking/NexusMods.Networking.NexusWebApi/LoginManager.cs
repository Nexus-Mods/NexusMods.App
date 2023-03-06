using System.Reactive.Linq;
using Microsoft.Extensions.Logging;
using NexusMods.Common.ProtocolRegistration;
using NexusMods.DataModel.Abstractions;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Component for handling login and logout from the Nexus Mods
/// </summary>
public class LoginManager
{
    private readonly ILogger<LoginManager> _logger;
    private readonly OAuth _oauth;
    private readonly IDataStore _dataStore;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly Client _client;
    private readonly OAuth2MessageFactory _msgFactory;
    public IObservable<UserInfo?> UserInfo { get; }

    /// <summary>
    /// True if the user is logged in
    /// </summary>
    public IObservable<bool> IsLoggedIn => UserInfo.Select(info => info != null);

    /// <summary>
    /// True if the user is logged in and is a premium member
    /// </summary>
    public IObservable<bool> IsPremium => UserInfo.Select(info => info?.IsPremium ?? false);

    /// <summary>
    /// The user's avatar
    /// </summary>
    public IObservable<Uri?> Avatar => UserInfo.Select(info => info?.Avatar);

    public LoginManager(ILogger<LoginManager> logger, Client client,
        OAuth2MessageFactory msgFactory,
        OAuth oauth, IDataStore dataStore, IProtocolRegistration protocolRegistration)
    {
        _logger = logger;
        _oauth = oauth;
        _msgFactory = msgFactory;
        _client = client;
        _dataStore = dataStore;
        _protocolRegistration = protocolRegistration;
        UserInfo = _dataStore.IdChanges
            .Where(id => id.Equals(JWTTokenEntity.StoreId))
            .Select(_ => true)
            .StartWith(true)
            .SelectMany(async _ => await Verify());
    }

    private async Task<UserInfo?> Verify()
    {
        if (await _msgFactory.IsAuthenticated())
            return await _msgFactory.Verify(_client, CancellationToken.None);
        return null;
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
        _dataStore.Put(JWTTokenEntity.StoreId, new JWTTokenEntity
        {
            RefreshToken = jwtToken.RefreshToken,
            AccessToken = jwtToken.AccessToken,
            Store = _dataStore
        });
    }

    /// <summary>
    ///  Log out of Nexus Mods
    /// </summary>
    public async Task Logout()
    {
        _dataStore.Delete(JWTTokenEntity.StoreId);
    }
}
