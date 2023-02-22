using NexusMods.CLI;
using NexusMods.DataModel.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NexusMods.Networking.NexusWebApi.Verbs;

/// <summary>
/// verb for logging out of the nexus api, which just means dropping our login
/// credentials since the server doesn't actually store anything about an api client
/// being logged in.
/// </summary>
public class NexusLogout : AVerb
{
    private readonly IDataStore _store;

    /// <inheritdoc/>
    public static VerbDefinition Definition => new("nexus-logout",
        "Drop login token for the Nexus API",
        Array.Empty<OptionDefinition>());

    /// <summary>
    /// constructor
    /// </summary>
    public NexusLogout(Configurator configurator, IDataStore store)
    {
        _store = store;
    }

    /// <inheritdoc/>
    public Task<int> Run(CancellationToken cancel)
    {
        // TODO should be deleting from the store but the store doesn't have that function yet
        _store.Put(JWTTokenEntity.StoreId, new JWTTokenEntity { AccessToken = "", RefreshToken = "", Store = _store });

        return Task.FromResult(0);
    }
}
