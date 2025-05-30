using System.Net.Http.Json;
using NexusMods.Backend.Stores.EpicGameStore.DTOs.EgData;

namespace NexusMods.Backend.Stores;

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
        var requestUrl = $"{BaseUrl}items/{appId}/builds";
        var result = await _httpClient.GetFromJsonAsync<Build[]>(requestUrl, token);
        if (result == null)
        {
            throw new HttpRequestException($"Failed to fetch builds for appId: {appId}");
        }
        return result;
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
