using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.Networking.NexusWebApi.Verbs;

/// <summary>
/// verb for establishing oauth 2 authorization to Nexus Mods
/// </summary>
public class NexusLogin : AVerb
{
    private readonly OAuth _oauth;
    private readonly IProtocolRegistration _protocolRegistration;
    private readonly IDataStore _store;

    /// <inheritdoc/>
    public static VerbDefinition Definition => new("nexus-login",
        "Acquire login token via the Nexus API",
        Array.Empty<OptionDefinition>());

    /// <summary>
    /// constructor
    /// </summary>
    public NexusLogin(Configurator configurator, IDataStore store, OAuth oauth, IProtocolRegistration protocolRegistration)
    {
        _oauth = oauth;
        _protocolRegistration = protocolRegistration;
        _store = store;
    }

    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancel)
    {
        // temporary but if we want oauth to work we _have_ to be registered as the nxm handler
        _protocolRegistration.RegisterSelf("nxm");

        var token = await _oauth.AuthorizeRequest(cancel);
        _store.Put(JWTTokenEntity.StoreId, new JWTTokenEntity
        {
            RefreshToken = token.RefreshToken,
            AccessToken = token.AccessToken,
            Store = _store
        });

        return 0;
    }
}
