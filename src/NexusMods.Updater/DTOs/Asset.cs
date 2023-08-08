using System.Text.Json.Serialization;

namespace NexusMods.Updater.DTOs;

public class Asset
{
    [JsonPropertyName("browser_download_url")]
    public Uri BrowserDownloadUrl { get; set; } = new("https://nexusmods.com/");

    [JsonPropertyName("name")] public string Name { get; set; } = "";


    [JsonPropertyName("size")]
    public long Size { get; set; }
}

