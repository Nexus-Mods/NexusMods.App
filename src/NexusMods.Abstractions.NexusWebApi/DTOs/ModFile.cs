using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using NexusMods.Abstractions.NexusWebApi.DTOs.Interfaces;
using NexusMods.Abstractions.NexusWebApi.Types;
using NexusMods.Abstractions.NexusWebApi.Types.V2;
using FileId = NexusMods.Abstractions.NexusWebApi.Types.V2.FileId;

// 👇 Suppress uninitialised variables. Currently Nexus has mostly read-only API and we expect server to return the data.
#pragma warning disable CS8618

// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace NexusMods.Abstractions.NexusWebApi.DTOs;

/// <summary>
/// Abstracts away an individual downloadable file available on a mod page.
/// </summary>
public class ModFile : IJsonSerializable<ModFile>
{
    /// <summary>
    ///    Listing of unique identifiers for this file.
    /// </summary>
    /// <remarks>
    ///    Currently this stores a tuple of FileId and GameId [encoded as array].
    /// </remarks>
    [JsonPropertyName("id")]
    public List<int> Id { get; set; }

    /// <summary>
    /// Unique [site-wide] ID for this file.
    /// </summary>
    [JsonPropertyName("uid")]
    public long Uid { get; set; }

    /// <summary>
    /// Unique ID for this file.
    /// </summary>
    /// <remarks>
    ///    This field is for (de)serialization only. Please use <see cref="FileId"/>.
    /// </remarks>
    [JsonPropertyName("file_id")]
    // ReSharper disable once InconsistentNaming
    public ulong _FileId { get; set; }

    /// <summary>
    /// Unique ID for this file.
    /// </summary>
    /// <remarks>
    ///    This ID is unique within the context of the game.
    ///    i.e. This ID might be used for another mod if you search for mods for another game.
    /// </remarks>
    public FileId FileId => FileId.From((uint)_FileId);

    /// <summary>
    /// Name (title) of the mod file as seen on the `Files` section of the mod page.
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Version of the mod.
    /// </summary>
    /// <remarks>
    ///    The Nexus site does not enforce validation on this field.
    ///    This field can be set to any arbitrary string
    /// </remarks>
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// Unique identifier to the <see cref="Category"/> this item is tied to.
    /// </summary>
    [JsonPropertyName("category_id")]
    public int CategoryId { get; set; }

    /// <summary>
    /// Name of the <see cref="Category"/> this item is tied to.
    /// </summary>
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; }

    /// <summary>
    /// True if this is the primary (i.e. 'main'/'top') download for this submission.
    /// </summary>
    [JsonPropertyName("is_primary")]
    public bool IsPrimary { get; set; }

    /// <summary>
    /// Size of this item in Kibibytes (KiB); rounded up to the nearest value.
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// Full name of this file as downloaded to the user's PC.
    /// </summary>
    /// <remarks>
    ///    This is (usually) composed of the original name of file appended with <see cref="ModId"/> and upload date/time.
    /// </remarks>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; }

    /// <summary>
    /// Unix timestamp for the time this file was uploaded.
    /// </summary>
    [JsonPropertyName("uploaded_timestamp")]
    public int UploadedTimestamp { get; set; }

    /// <summary>
    /// Specifies when this mod was uploaded.
    /// This is equivalent to <see cref="UploadedTimestamp"/>.
    /// </summary>
    /// <remarks>
    ///    Expressed as ISO 8601 compatible date/time notation.
    /// </remarks>
    [JsonPropertyName("uploaded_time")]
    public DateTime UploadedTime { get; set; }

    /// <summary>
    /// Version of the mod. See <see cref="Version"/> for more details.
    /// </summary>
    [JsonPropertyName("mod_version")]
    public string ModVersion { get; set; }

    /// <summary>
    /// URL for a 3rd party (VirusTotal) scan for the uploaded files.
    /// </summary>
    [JsonPropertyName("external_virus_scan_url")]
    public string ExternalVirusScanUrl { get; set; }

    /// <summary>
    /// Description for this individual downloaded item. Usually appears under file title.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// (Same as <see cref="Size"/>). Size of this item in Kibibytes (KiB); rounded up to the nearest value.
    /// </summary>
    /// <remarks>
    ///    Uses KiB (1 KiB = 1024) bytes; not KB (1000 bytes).
    /// </remarks>
    [JsonPropertyName("size_kb")]
    public int SizeKb { get; set; }

    /// <summary>
    /// Full size of the file expressed as bytes.
    /// </summary>
    [JsonPropertyName("size_in_bytes")]
    // ReSharper disable once InconsistentNaming
    public long? SizeInBytes { get; set; }

    /// <summary>
    /// The changelog for this item, expressed as raw HTML.
    /// </summary>
    [JsonPropertyName("changelog_html")]
    public string ChangelogHtml { get; set; }

    /// <summary>
    /// Link to a .json file which specifies all of the files inside this uploaded archive.
    /// </summary>
    [JsonPropertyName("content_preview_link")]
    public string ContentPreviewLink { get; set; }

    /// <inheritdoc />
    public static JsonTypeInfo<ModFile> GetTypeInfo() => ModFileContext.Default.ModFile;
}

/// <summary/>
[JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(ModFile))]
public partial class ModFileContext : JsonSerializerContext { }
