using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.DTOs;

/// <summary>
/// Defines the features of a download server. 
/// </summary>
class ServerFeatures
{
    /// <summary>
    /// Not technically a feature, servers report the size of the download as part of this request
    /// </summary>
    public Size? DownloadSize { get; init; }
    
    public bool SupportsResume { get; init; }

    public static async Task<ServerFeatures> Request(HttpClient client, HttpRequestMessage message, CancellationToken token)
    {
        message.Method = HttpMethod.Head;
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Server responded with {response.StatusCode}");
        }
        
        var features = new ServerFeatures
        {
            SupportsResume = response.Headers.AcceptRanges.Contains("bytes"),
            DownloadSize = response.Content.Headers.ContentLength == null ? 
                null : 
                Size.FromLong(response.Content.Headers.ContentLength.Value)
        };
        return features;
    }
}
