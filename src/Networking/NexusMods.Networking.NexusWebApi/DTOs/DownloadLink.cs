using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Networking.NexusWebApi.DTOs.Interfaces;
using NexusMods.Networking.NexusWebApi.Types;

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Networking.NexusWebApi.DTOs;

/// <summary>
/// Represents an individual download link returned from the API.
/// </summary>
/// <remarks>
///    At the current moment in time; only premium users can receive this; with the exception of NXM links.
/// </remarks>
public class DownloadLink : IJsonArraySerializable<DownloadLink>
{
    /// <summary/>
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// For deserialization only. Please use <see cref="ShortName"/>.
    /// </summary>
    [JsonPropertyName("short_name")]
    public string _ShortName { get; set; } = null!;

    /// <summary>
    /// Name of the CDN server that handles your download request.
    /// </summary>
    public CDNName ShortName => CDNName.From(_ShortName);

    /// <summary>
    /// Download URI used to download the files.
    /// </summary>
    [JsonPropertyName("URI")]
    public Uri Uri { get; set; } = null!;

    /// <inheritdoc />
    public static JsonTypeInfo<DownloadLink[]> GetArrayTypeInfo() => DownloadLinkArrayContext.Default.DownloadLinkArray;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(DownloadLink[]))]
public partial class DownloadLinkArrayContext : JsonSerializerContext { }
