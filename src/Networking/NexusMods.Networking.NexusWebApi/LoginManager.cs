using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;

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

    public LoginManager(ILogger<LoginManager> logger, OAuth oauth, IDataStore dataStore, IProtocolRegistration protocolRegistration)
    {
        _logger = logger;
        _oauth = oauth;
        _dataStore = dataStore;
        _protocolRegistration = protocolRegistration;
    }
    
    /// <summary>
    /// Show a browser and log into Nexus Mods
    /// </summary>
    /// <param name="token"></param>
    public async Task LoginAsync(CancellationToken token = default)
    {
        // temporary but if we want oauth to work we _have_ to be registered as the nxm handler
        _protocolRegistration.RegisterSelf("nxm");

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