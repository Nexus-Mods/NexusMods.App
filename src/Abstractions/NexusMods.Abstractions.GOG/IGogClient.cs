using NexusMods.Abstractions.GOG.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Abstractions.GOG;

public interface IGogClient
{
    /// <summary>
    /// Notify the client of a new auth url being passed in from the CLI.
    /// </summary>
    public void AuthUrl(NXMGogAuthUrl url);
}
