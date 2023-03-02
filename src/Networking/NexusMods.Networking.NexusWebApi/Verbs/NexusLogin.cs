using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.DataModel.Abstractions;

namespace NexusMods.Networking.NexusWebApi.Verbs;

/// <summary>
/// verb for establishing oauth 2 authorization to Nexus Mods
/// </summary>
public class NexusLogin : AVerb
{
    private readonly LoginManager _loginManager;

    /// <inheritdoc/>
    public static VerbDefinition Definition => new("nexus-login",
        "Acquire login token via the Nexus API",
        Array.Empty<OptionDefinition>());

    /// <summary>
    /// constructor
    /// </summary>
    public NexusLogin(LoginManager loginManager)
    {
        _loginManager = loginManager;
    }

    /// <inheritdoc/>
    public async Task<int> Run(CancellationToken cancel)
    {
        await _loginManager.LoginAsync(cancel);
        return 0;
    }
}
