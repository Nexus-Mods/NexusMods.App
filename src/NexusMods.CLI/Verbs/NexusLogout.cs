using NexusMods.Abstractions.CLI;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Networking.NexusWebApi.NMA;

namespace NexusMods.CLI.Verbs;

/// <summary>
/// verb for logging out of the nexus api, which just means dropping our login
/// credentials since the server doesn't actually store anything about an api client
/// being logged in.
/// </summary>
public class NexusLogout : AVerb
{
    private readonly LoginManager _loginManager;

    /// <inheritdoc/>
    public static VerbDefinition Definition => new("nexus-logout",
        "Drop login token for the Nexus API",
        Array.Empty<OptionDefinition>());

    /// <summary>
    /// constructor
    /// </summary>
    public NexusLogout(LoginManager loginManager)
    {
        _loginManager = loginManager;
    }

    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancel)
    {
        await _loginManager.Logout();
        return 0;
    }
}
