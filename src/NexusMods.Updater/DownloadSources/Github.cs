using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Updater.DTOs;

namespace NexusMods.Updater.DownloadSources;

public class Github
{
    private readonly HttpClient _client;

    public Github(HttpClient client)
    {
        _client = client;
    }

    public async Task<Release?> GetLatestRelease(string owner, string repo)
    {
        var releases = await GetReleases(owner, repo);
        return releases.FirstOrDefault();
    }

    private async Task<Release[]> GetReleases(string owner, string repo)
    {
        var url = $"https://api.github.com/repos/{owner}/{repo}/releases";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "NexusMods.Updater");
        var response = await _client.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<Release[]>(json)!;
    }
}
