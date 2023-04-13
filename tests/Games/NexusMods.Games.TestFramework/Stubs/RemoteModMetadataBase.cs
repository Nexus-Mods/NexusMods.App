using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.Games.TestFramework.Stubs;

/// <summary>
/// Base class for remotely downloaded mods.
/// </summary>
public class RemoteModMetadataBase
{
    private static JsonSerializerOptions _options = new()
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Where this mod is sourced from.
    /// Defines the concrete type used.
    /// </summary>
    public RemoteModSource Source { get; set; }

    /// <summary>
    /// User friendly name for this mod.
    /// </summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// Deserializes a file at the given location.
    /// </summary>
    /// <param name="manifestPath">Absolute path to the file.</param>
    /// <param name="shared">Whether this is shared or not.</param>
    public static async Task<RemoteModMetadataBase> DeserializeFromAsync(AbsolutePath manifestPath, IFileSystem shared)
    {
        var text = await shared.ReadAllTextAsync(manifestPath);
        var baseItem = JsonSerializer.Deserialize<RemoteModMetadataBase>(text, _options);
        switch (baseItem!.Source)
        {
            case RemoteModSource.NexusMods:
                return JsonSerializer.Deserialize<NexusModMetadata>(text, _options)!;
            case RemoteModSource.RealFileSystem:
                var result = JsonSerializer.Deserialize<FileSystemModMetadata>(text, _options)!;
                result.JsonPath = manifestPath;
                return result;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
