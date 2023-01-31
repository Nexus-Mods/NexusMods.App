using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.Games;
using NexusMods.Networking.NexusWebApi.DTOs;
using NexusMods.Networking.NexusWebApi.Types;

namespace NexusMods.Networking.NexusWebApi;

public class Client
{
    private readonly ILogger<Client> _logger;
    private readonly IHttpMessageFactory _factory;
    private readonly HttpClient _httpClient;

    public Client(ILogger<Client> logger, IHttpMessageFactory factory, HttpClient httpClient)
    {
        _logger = logger;
        _factory = factory;
        _httpClient = httpClient;
    }

    public async Task<Response<ValidateInfo>> Validate(CancellationToken token)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri("https://api.nexusmods.com/v1/users/validate.json"));
        return await SendAsync<ValidateInfo>(msg, token);
    }
    
    public async Task<Response<GameInfo[]>> Games(CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri("https://api.nexusmods.com/v1/games.json"));
        return await SendAsync<GameInfo[]>(msg, token);
    }

    public async Task<Response<DownloadLink[]>> DownloadLinks(GameDomain domain, ModId modId, FileId fileId, CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri(
            $"https://api.nexusmods.com/v1/games/{domain}/mods/{modId}/files/{fileId}/download_link.json"));
        return await SendAsync<DownloadLink[]>(msg, token);
    }

    public enum PastTime
    {
        Day,
        Week,
        Month,
    }
    
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

        return await SendAsync<ModUpdate[]>(msg, token: token);

    }
    
    

    public async Task<Response<ModFiles>> ModFiles(GameDomain staticDomain, ModId modModId, CancellationToken token = default)
    {
        var msg = await _factory.Create(HttpMethod.Get, new Uri(
            $"https://api.nexusmods.com/v1/games/{staticDomain}/mods/{modModId}/files.json"));
        return await SendAsync<ModFiles>(msg, token);
    }

    private async Task<Response<T>> SendAsync<T>(HttpRequestMessage message,
        CancellationToken token = default)
    {
        using var response = await _httpClient.SendAsync(message, token);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(response.ReasonPhrase, null, response.StatusCode);
        
        var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken: token);
        return new Response<T>
        {
            Data = data,
            Metadata = ParseHeaders(response),
            StatusCode = response.StatusCode
        };
    }
    
    protected virtual ResponseMetadata ParseHeaders(HttpResponseMessage result)
    {
        var metaData = new ResponseMetadata();

        {
            if (result.Headers.TryGetValues("x-rl-daily-limit", out var limits))
                if (int.TryParse(limits.First(), out var limit))
                    metaData.DailyLimit = limit;
        }

        {
            if (result.Headers.TryGetValues("x-rl-daily-remaining", out var limits))
                if (int.TryParse(limits.First(), out var limit))
                    metaData.DailyRemaining = limit;
        }

        {
            if (result.Headers.TryGetValues("x-rl-daily-reset", out var resets))
                if (DateTime.TryParse(resets.First(), out var reset))
                    metaData.DailyReset = reset;
        }

        {
            if (result.Headers.TryGetValues("x-rl-hourly-limit", out var limits))
                if (int.TryParse(limits.First(), out var limit))
                    metaData.HourlyLimit = limit;
        }

        {
            if (result.Headers.TryGetValues("x-rl-hourly-remaining", out var limits))
                if (int.TryParse(limits.First(), out var limit))
                    metaData.HourlyRemaining = limit;
        }

        {
            if (result.Headers.TryGetValues("x-rl-hourly-reset", out var resets))
                if (DateTime.TryParse(resets.First(), out var reset))
                    metaData.HourlyReset = reset;
        }


        {
            if (result.Headers.TryGetValues("x-runtime", out var runtimes))
                if (double.TryParse(runtimes.First(), out var reset))
                    metaData.Runtime = reset;
        }

        _logger.LogInformation("Nexus API call finished: {Runtime} - Remaining Limit: {RemainingLimit}",
            metaData.Runtime, Math.Max(metaData.DailyRemaining, metaData.HourlyRemaining));

        return metaData;
    }
}