using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework.Stubs;

/// <summary>
/// Metadata used for downloading mods from the FileSystem.
/// </summary>
public class FileSystemModMetadata : RemoteModMetadataBase
{
    /// <summary>
    /// Path to the archive file relative to the folder the JSON is contained in.
    /// </summary>
    public string FilePath { get; set; } = "";

    /// <summary>
    /// This field is set at deserialization time.
    /// It is the physical location of the JSON.
    /// </summary>
    [JsonIgnore]
    public AbsolutePath JsonPath { get; set; }
}
