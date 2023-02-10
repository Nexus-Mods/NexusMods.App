using System.Text.Json.Serialization;
using NexusMods.Networking.NexusWebApi.Types;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.DTOs;

public class ModFile
{
    [JsonPropertyName("id")]
    public List<int> Id { get; set; }

    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    [JsonPropertyName("file_id")]
    public FileId FileId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; }

    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    [JsonPropertyName("uploaded_timestamp")]
    public int UploadedTimestamp { get; set; }

    [JsonPropertyName("uploaded_time")]
    public DateTime UploadedTime { get; set; }

    [JsonPropertyName("mod_version")]
    public string ModVersion { get; set; }

    [JsonPropertyName("external_virus_scan_url")]
    public string ExternalVirusScanUrl { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; }

    [JsonPropertyName("size_kb")]
    public int SizeKb { get; set; }

    [JsonPropertyName("size_in_bytes")]
    public Size? SizeInBytes { get; set; }

    [JsonPropertyName("changelog_html")]
    public string ChangelogHtml { get; set; }

    [JsonPropertyName("content_preview_link")]
    public string ContentPreviewLink { get; set; }
}