using NexusMods.Abstractions.NexusWebApi.DTOs;
using NexusMods.Abstractions.NexusWebApi.DTOs.OAuth;
using NexusMods.Abstractions.NexusWebApi.Types;
using FileId = NexusMods.Abstractions.NexusWebApi.Types.V2.FileId;
using ModId = NexusMods.Abstractions.NexusWebApi.Types.V2.ModId;

namespace NexusMods.Abstractions.NexusWebApi;

/// <summary>
/// Interface for the Nexus Web API Client.
/// </summary>
public interface INexusApiClient
{
    /// <summary>
    /// Retrieves the current user information when logged in via APIKEY
    /// </summary>
    /// <param name="token">Can be used to cancel this task.</param>
    Task<Response<ValidateInfo>> Validate(CancellationToken token = default);

    /// <summary>
    /// Retrieves information about the current user when logged in via OAuth.
    /// </summary>
    Task<Response<OAuthUserInfo>> GetOAuthUserInfo(CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates download links for a given game.
    /// [Premium only endpoint, use other overload for free users].
    /// </summary>
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
    Task<Response<DownloadLink[]>> DownloadLinksAsync(string domain, ModId modId, FileId fileId, CancellationToken token = default);

    /// <summary>
    /// Generates download links for a given game.
    /// </summary>
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
    /// <param name="expireTime">Time before key expires.</param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <param name="key">Key required for free user to download from the site.</param>
    /// <returns> List of available download links. </returns>
    /// <remarks>
    ///    Currently available for Premium users only; with some minor exceptions [nxm links].
    /// </remarks>
    Task<Response<DownloadLink[]>> DownloadLinksAsync(string domain, ModId modId, FileId fileId, NXMKey key, DateTime expireTime, CancellationToken token = default);


    /// <summary>
    /// Get the download links for a collection.
    /// </summary>
    Task<Response<CollectionDownloadLinks>> CollectionDownloadLinksAsync(CollectionSlug slug, RevisionNumber revision,  bool viewAdultContent = false, CancellationToken token = default);

    /// <summary>
    /// Retrieves a list of all recently updated mods within a specified time period.
    /// </summary>
    /// <param name="domain">
    ///     Unique, human friendly name for the game used in URLs. e.g. 'skyrim'
    ///     You can find this in <see cref="GameInfo.DomainName"/>.
    /// </param>
    /// <param name="time">Time-frame within which to search for updates.</param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    Task<Response<ModUpdate[]>> ModUpdatesAsync(string domain, PastTime time, CancellationToken token = default);
}
