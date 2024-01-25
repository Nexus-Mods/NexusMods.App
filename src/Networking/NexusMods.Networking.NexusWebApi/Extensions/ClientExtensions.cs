using NexusMods.Abstractions.Games.DTO;
using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi.Extensions;

/// <summary>
/// Extensions that integrate the Web API with NMA's data types.
/// </summary>
public static class ClientExtensions
{
    /// <summary>
    /// Generates download links for a given game.
    /// </summary>
    /// <param name="nexusApiClient">The nexusApiClient which to run the operation on.</param>
    /// <param name="domain">
    ///     Unique, human friendly name for the game used in URLs. e.g. 'skyrim'
    ///     You can find this in <see cref="GameInfo.DomainName"/>.
    /// </param>
    /// <param name="modId">
    ///    An individual identifier for the mod. Unique per game.
    /// </param>
    /// <param name="fileId">
    ///    Unique ID for a game file hosted on a mod page; unique per game.
    /// </param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <returns> List of available download links. </returns>
    /// <remarks>
    ///    Currently available for Premium users only; with some minor exceptions [nxm links].
    /// </remarks>
    public static async Task<Response<DownloadLink[]>> DownloadLinksAsync(this NexusApiClient nexusApiClient, GameDomain domain, ModId modId, FileId fileId, CancellationToken token = default)
    {
        return await nexusApiClient.DownloadLinksAsync(domain.Value, modId, fileId, token);
    }

    /// <summary>
    /// Retrieves a list of all recently updated mods within a specified time period.
    /// </summary>
    /// <param name="nexusApiClient">The nexusApiClient which to run the operation on.</param>
    /// <param name="domain">
    ///     Unique, human friendly name for the game used in URLs. e.g. 'skyrim'
    ///     You can find this in <see cref="GameInfo.DomainName"/>.
    /// </param>
    /// <param name="time">Time-frame within which to search for updates.</param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static async Task<Response<ModUpdate[]>> ModUpdatesAsync(this NexusApiClient nexusApiClient, GameDomain domain, PastTime time, CancellationToken token = default)
    {
        return await nexusApiClient.ModUpdatesAsync(domain.Value, time, token);
    }

    /// <summary>
    /// Returns all of the downloadable files associated with a mod.
    /// </summary>
    /// <param name="nexusApiClient">The nexusApiClient which to run the operation on.</param>
    /// <param name="domain">
    ///     Unique, human friendly name for the game used in URLs. e.g. 'skyrim'
    ///     You can find this in <see cref="GameInfo.DomainName"/>.
    /// </param>
    /// <param name="modId">
    ///    An individual identifier for the mod. Unique per game.
    /// </param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <returns></returns>
    public static async Task<Response<ModFiles>> ModFilesAsync(this NexusApiClient nexusApiClient, GameDomain domain, ModId modId, CancellationToken token = default)
    {
        return await nexusApiClient.ModFilesAsync(domain.Value, modId, token);
    }
}
