using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;
using NexusMods.Paths.Extensions;
using NexusMods.Paths.Utilities;

namespace NexusMods.Paths;

/// <summary>
/// Path used for configuration files.<br/>
/// This path has the following properties:<br/>
/// - Can be relative or absolute.<br/>
/// - Is automatically expanded.<br/>
/// - Implicit cast back to string for convenience.<br/>
/// </summary>
/// <remarks>
/// If we were ever to provide a UI for editing settings;
/// converting it to full path during the serialize/deserialize
/// step would be lossy as we would lose the expansion monikers.<br/>
///
/// Using this struct we can keep the original string around for the ride too.
/// </remarks>
[PublicAPI]
[JsonConverter(typeof(ConfigurationPathJsonConverter))]
public struct ConfigurationPath : IEquatable<ConfigurationPath>
{
    /// <summary>
    /// Raw, unmodified path.
    /// </summary>
    public string RawPath { get; set; }

    /// <summary>
    /// The filesystem used by this path when it constructs absolute paths.
    /// </summary>
    public IFileSystem FileSystem { get; }

    /// <summary>
    /// Creates a new configuration path.
    /// </summary>
    /// <param name="rawPath">Raw path which can include expansion monikers.</param>
    /// <param name="fileSystem">The filesystem to use when converting to AbsolutePaths</param>
    public ConfigurationPath(string rawPath, IFileSystem fileSystem)
    {
        RawPath = rawPath;
        FileSystem = fileSystem;
    }

    /// <summary>
    /// Creates a new configuration path, with the default global filesystem
    /// </summary>
    /// <param name="path"></param>
    public ConfigurationPath(AbsolutePath path)
    {
        RawPath = path.GetFullPath();
        FileSystem = path.FileSystem;
    }

    /// <summary>
    /// Retrieves the full path behind this configuration parameter.
    /// </summary>
    public string GetFullPath() => FileSystem.ExpandKnownFoldersPath(RawPath);

    /// <inheritdoc />
    public override string ToString() => GetFullPath();

    /// <summary>
    /// Converts the current string to an absolute path.
    /// </summary>
    public AbsolutePath ToAbsolutePath() => AbsolutePath.FromUnsanitizedFullPath(GetFullPath(), FileSystem);

    /// <inheritdoc />
    public bool Equals(ConfigurationPath other) => RawPath == other.RawPath;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ConfigurationPath other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => RawPath.GetHashCode();
}

/// <summary>
/// Converter for the <see cref="ConfigurationPath"/> type which serializes directly as string.
/// </summary>
public class ConfigurationPathJsonConverter : JsonConverter<ConfigurationPath>
{
    private readonly IFileSystem _fileSystem;

    /// <summary>
    /// Default constructor.
    /// </summary>
    public ConfigurationPathJsonConverter()
    {
        _fileSystem = FileSystem.Shared;
    }

    /// <summary>
    /// DI constructor.
    /// </summary>
    /// <param name="fileSystem"></param>
    public ConfigurationPathJsonConverter(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public override ConfigurationPath Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            ThrowHelpers.JsonException("Expected a string value for ConfigurationPath.");

        var rawPath = reader.GetString();
        return new ConfigurationPath(rawPath!, _fileSystem);
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, ConfigurationPath value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.RawPath);
    }
}
