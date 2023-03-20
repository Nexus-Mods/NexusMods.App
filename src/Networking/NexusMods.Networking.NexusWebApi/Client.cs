using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.DTOs.Interfaces;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

/// <summary>
/// Provides an easy to use access point for the Nexus API; start your journey here.
/// </summary>
public class Client
{
    private readonly ILogger<Client> _logger;
    private readonly IHttpMessageFactory _factory;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates a <see cref="Client"/> responsible for providing easy access to the Nexus API.
    /// </summary>
    /// <param name="logger">Logs actions performed by the client.</param>
    /// <param name="factory">Injects API key into the messages.</param>
    /// <param name="httpClient">Client used to issue HTTP requests.</param>
    /// <remarks>
    ///    This class is usually instantiated using the Microsoft DI Container.
    /// </remarks>
    public Client(ILogger<Client> logger, IHttpMessageFactory factory, HttpClient httpClient)
    {
        _logger = logger;
        _factory = factory;
        _httpClient = httpClient;
    }

    /// <summary>
    /// Retrieves the current user information when logged in via APIKEY
    /// </summary>
    /// <param name="token">Can be used to cancel this task.</param>
    public async Task<Response<ValidateInfo>> Validate(CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri("https://api.nexusmods.com/v1/users/validate.json"));
        return await SendAsync<ValidateInfo>(msg, token);
    }

    /// <summary>
    /// Returns a list of games supported by Nexus.
    /// </summary>
    /// <param name="token">Can be used to cancel this task.</param>
    public async Task<Response<GameInfo[]>> Games(CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri("https://api.nexusmods.com/v1/games.json"));
        return await SendAsyncArray<GameInfo>(msg, token);
    }

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
    /// <param name="token">Token used to cancel the task.</param>
    /// <returns> List of available download links. </returns>
    /// <remarks>
    ///    Currently available for Premium users only; with some minor exceptions [nxm links].
    /// </remarks>
    public async Task<Response<DownloadLink[]>> DownloadLinks(GameDomain domain, ModId modId, FileId fileId, CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri(
            $"https://api.nexusmods.com/v1/games/{domain}/mods/{modId}/files/{fileId}/download_link.json"));
        return await SendAsyncArray<DownloadLink>(msg, token);
    }

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
    public async Task<Response<ModUpdate[]>> ModUpdates(GameDomain domain, PastTime time, CancellationToken token = default)
    {
        var timeString = time switch
        {
            PastTime.Day => "1d",
            PastTime.Week => "1w",
            PastTime.Month => "1m",
            _ => throw new ArgumentOutOfRangeException(nameof(time), time, null)
        };

        var msg = await _factory.Create(HttpMethod.Get, new Uri(
            $"https://api.nexusmods.com/v1/games/{domain}/mods/updated.json?period={timeString}"));

        return await SendAsyncArray<ModUpdate>(msg, token: token);
    }

    /// <summary>
    /// Returns all of the downloadable files associated with a mod.
    /// </summary>
    /// <param name="domain">
    ///     Unique, human friendly name for the game used in URLs. e.g. 'skyrim'
    ///     You can find this in <see cref="GameInfo.DomainName"/>.
    /// </param>
    /// <param name="modId">
    ///    An individual identifier for the mod. Unique per game.
    /// </param>
    /// <param name="token">Token used to cancel the task.</param>
    /// <returns></returns>
    public async Task<Response<ModFiles>> ModFiles(GameDomain domain, ModId modId, CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri(
            $"https://api.nexusmods.com/v1/games/{domain}/mods/{modId}/files.json"));
        return await SendAsync<ModFiles>(msg, token);
    }

    private async Task<Response<T>> SendAsync<T>(HttpRequestMessage message,
        CancellationToken token = default) where T : IJsonSerializable<T>
    {
        return await SendAsync(message, T.GetTypeInfo(), token);
    }

    private async Task<Response<T[]>> SendAsyncArray<T>(HttpRequestMessage message,
        CancellationToken token = default) where T : IJsonArraySerializable<T>
    {
        return await SendAsync(message, T.GetArrayTypeInfo(), token);
    }

    private async Task<Response<T>> SendAsync<T>(HttpRequestMessage message, JsonTypeInfo<T> typeInfo,
        CancellationToken token = default)
    {
        try
        {
            using var response = await _httpClient.SendAsync(message, token);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);

            var data = await response.Content.ReadFromJsonAsync(typeInfo, token);
            return new Response<T>
            {
                Data = data!,
                Metadata = ParseHeaders(response),
                StatusCode = response.StatusCode
            };
        }
        catch (HttpRequestException ex)
        {
            var newMessage = await _factory.HandleError(message, ex, token);
            if (newMessage != null)
            {
                return await SendAsync(newMessage, typeInfo, token);
            }

            throw;
        }
    }

    private ResponseMetadata ParseHeaders(HttpResponseMessage result)
    {
        var metaData = ResponseMetadata.FromHttpHeaders(result);

        _logger.LogInformation("Nexus API call finished: {Runtime} - Remaining Limit: {RemainingLimit}",
            metaData.Runtime, Math.Max(metaData.DailyRemaining, metaData.HourlyRemaining));

        return metaData;
    }

    /// <summary>
    /// Specifies the time period used to search for items.
    /// </summary>
    public enum PastTime
    {
        /// <summary>
        /// Searches the past 24 hours.
        /// </summary>
        Day,

        /// <summary>
        /// Searches the past 7 days.
        /// </summary>
        Week,

        /// <summary>
        /// Searches the past month.
        /// </summary>
        Month,
    }
}
