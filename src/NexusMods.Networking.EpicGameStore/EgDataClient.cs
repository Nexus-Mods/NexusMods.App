using System.Net;
using System.Net.Http.Json;
using NexusMods.Networking.EpicGameStore.DTOs.EgData;

namespace NexusMods.Networking.EpicGameStore;

public class EgDataClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api-gcp.egdata.app/";

    public EgDataClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Build[]> GetBuilds(string appId, CancellationToken token = default)
    {
        // egdata.app serves DLC via `items` and games via `offers`
        var itemsUrl = $"{BaseUrl}items/{appId}/builds";
        var offersUrl = $"{BaseUrl}offers/{appId}/builds";
        
        Exception? exception = null;
        foreach (var url in new[] { offersUrl, itemsUrl })
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<Build[]>(url, token);
                if (result == null)
                {
                    throw new HttpRequestException($"Failed to fetch builds for appId: {appId}");
                }

                return result;
            }
            catch (HttpRequestException ex)
            {
                exception = ex;
                if (ex.StatusCode == HttpStatusCode.NotFound)
                    continue;
            }
        }
        throw exception ?? new HttpRequestException($"Failed to fetch builds for appId: {appId}");
    }
    
    public async Task<BuildFile[]> GetFiles(string buildId, CancellationToken token = default)
    {
        var requestUrl = $"{BaseUrl}builds/{buildId}/files?limit=10000";
        var result = await _httpClient.GetFromJsonAsync<BuildFiles>(requestUrl, token);
        if (result == null)
        {
            throw new HttpRequestException($"Failed to fetch files for buildId: {buildId}");
        }
        if (result.Total != result.Files.Length)
            throw new NotImplementedException("Support for pagination is not implemented yet.");
        
        return result.Files;
    }
}
