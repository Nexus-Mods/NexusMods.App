using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.GOG;

/// <summary>
/// Interface for the GOG API client.
/// </summary>
public interface IClient
{
    /// <summary>
    /// Notify the client of a new auth url being passed in from the CLI.
    /// </summary>
    public void AuthUrl(NXMGogAuthUrl url);

    /// <summary>
    /// Initiate the login process.
    /// </summary>
    public Task Login(CancellationToken token);
}
