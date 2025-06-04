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
    public readonly KnownPath? BaseDirectory;

    /// <summary>
    /// The file part.
    /// </summary>
    public readonly string File;

    /// <summary>
    /// Constructor.
    /// </summary>
    public ConfigurablePath(KnownPath? baseDirectory, string file)
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

        if (BaseDirectory == null)
            return fileSystem.FromUnsanitizedFullPath(File);
        return fileSystem.GetKnownPath(BaseDirectory!.Value).Combine(File);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return $"{BaseDirectory}/{File}";
    }

    /// <inheritdoc/>
    public class JsonConverter : JsonConverter<ConfigurablePath>
    {
        private record ConfigurablePathJson(string? BaseDirectory, string File);
        
        /// <inheritdoc/>
        public override ConfigurablePath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var obj = JsonSerializer.Deserialize<ConfigurablePathJson>(ref reader, options);
            if (obj == null)
                throw new JsonException("Failed to deserialize ConfigurablePathJson");
            if (!string.IsNullOrWhiteSpace(obj.BaseDirectory))
                return new ConfigurablePath(
                    Enum.Parse<KnownPath>(obj.BaseDirectory),
                    obj.File
                );
            
            return new ConfigurablePath(null, obj.File);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, ConfigurablePath value, JsonSerializerOptions options)
        {
            var obj = new ConfigurablePathJson(
                value.BaseDirectory?.ToString(),
                value.File
            );
            
            JsonSerializer.Serialize(writer, obj, options);
        }
    }
}
