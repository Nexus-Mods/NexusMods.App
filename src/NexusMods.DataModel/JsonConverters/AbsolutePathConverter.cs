using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;

namespace NexusMods.DataModel.JsonConverters;

/// <summary>
/// Json converter for <see cref="AbsolutePath"/>.
/// </summary>
public class AbsolutePathConverter : JsonConverter<AbsolutePath>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// DI Constructor
    /// </summary>
    /// <param name="fileSystem"></param>
    public AbsolutePathConverter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    /// <inheritdoc />
    public override AbsolutePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return _fileSystem.FromUnsanitizedFullPath(reader.GetString()!);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AbsolutePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
