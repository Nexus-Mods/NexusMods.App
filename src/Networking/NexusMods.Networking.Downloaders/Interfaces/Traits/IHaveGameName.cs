namespace NexusMods.Networking.Downloaders.Interfaces.Traits;

/// <summary>
/// Interface implemented by <see cref="IDownloadTask"/>(s) that have a known game name.
/// </summary>
/// <remarks>
///     This is separated out today because most likely loadout selection will become a separate step down the road.
/// </remarks>
public interface IHaveGameName
{
    /// <summary>
    /// Name of the game the mod will be installed to.
    /// </summary>
    public string Version { get; }
}
