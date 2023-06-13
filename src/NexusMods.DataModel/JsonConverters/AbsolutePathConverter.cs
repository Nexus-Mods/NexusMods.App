using System.Text.Json;
using System.Text.Json.Serialization;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;

namespace NexusMods.DataModel.JsonConverters;

/// <inheritdoc />
public class AbsolutePathConverter : JsonConverter<AbsolutePath>
{
    private readonly IFileSystem _fileSystem;

    public AbsolutePathConverter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }
    
    /// <inheritdoc />
    public override AbsolutePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString()!.ToAbsolutePath(_fileSystem);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, AbsolutePath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
