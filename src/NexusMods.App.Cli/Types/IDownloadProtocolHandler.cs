using NexusMods.Abstractions.Loadouts;
using NexusMods.Sdk.Loadouts;

namespace NexusMods.CLI.Types;

/// <summary>
/// Defines a protocol handler used for downloading items.
/// </summary>
public interface IDownloadProtocolHandler
{
    /// <summary>
    /// The protocol to handle, e.g. 'nxm'
    /// </summary>
    public string Protocol { get; }

    /// <summary>
    /// Handles downloads from the given URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <param name="modName">Name of the mod to install.</param>
    /// <param name="token">Allows to cancel the operation.</param>
    /// <param name="loadout">Load to install the mod to.</param>
    public Task Handle(Uri url, Loadout.ReadOnly loadout, string modName, CancellationToken token);
}
