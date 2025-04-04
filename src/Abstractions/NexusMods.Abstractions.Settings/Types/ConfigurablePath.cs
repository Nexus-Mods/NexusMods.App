using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.Abstractions.Settings;

/// <summary>
/// Represents a configurable path. This is made up of a <see cref="KnownPath"/>
/// base directory and a <see cref="RelativePath"/> file part.
/// </summary>
[PublicAPI]
[JsonConverter(typeof(JsonConverter))]
public readonly struct ConfigurablePath
{
    /// <summary>
    /// The base directory part.
    /// </summary>
    public readonly KnownPath BaseDirectory;

    /// <summary>
    /// The file part.
    /// </summary>
    public readonly RelativePath File;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigurablePath(KnownPath baseDirectory, RelativePath file)
    {
        BaseDirectory = baseDirectory;
        File = file;
    }

    /// <summary>
    /// Converts to a <see cref="AbsolutePath"/>.
    /// </summary>
    public AbsolutePath ToPath(IFileSystem fileSystem)
    {
        if (BaseDirectory == default(KnownPath) && File == default(RelativePath))
            throw new InvalidOperationException($"This {nameof(ConfigurablePath)} contains only default values!");

        return fileSystem.GetKnownPath(BaseDirectory).Combine(File);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{BaseDirectory}/{File}";
    }

    /// <inheritdoc/>
    public class JsonConverter : JsonConverter<ConfigurablePath>
    {
        /// <inheritdoc/>
        public override ConfigurablePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException();
            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            reader.Read();

            if (reader.TokenType != JsonTokenType.String) throw new JsonException();
            var baseDirectoryString = reader.GetString();
            reader.Read();

            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();
            reader.Read();

            if (reader.TokenType != JsonTokenType.String) throw new JsonException();
            var fileString = reader.GetString();
            reader.Read();

            if (reader.TokenType != JsonTokenType.EndObject) throw new JsonException();

            if (!Enum.TryParse<KnownPath>(baseDirectoryString, ignoreCase: true, out var baseDirectory))
                throw new JsonException($"Unknown: {baseDirectoryString}");

            if (fileString is null) throw new JsonException("File can't be null");
            return new ConfigurablePath(baseDirectory, fileString);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ConfigurablePath value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();

            writer.WriteString($"{nameof(BaseDirectory)}", value.BaseDirectory.ToString());
            writer.WriteString($"{nameof(File)}", value.File.ToString());

            writer.WriteEndObject();
        }
    }
}
