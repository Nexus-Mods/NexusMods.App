using System.Text;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;

namespace NexusMods.Backend;

[UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
public sealed class JsonStorageBackend : IStorageBackend
{
    public StorageBackendId Id { get; } = StorageBackends.Json;

    private readonly ILogger _logger;
    private readonly Lazy<JsonSerializerOptions> _jsonOptions;
    private readonly AbsolutePath _configDirectory;

    /// <summary>
    /// Constructor.
    /// </summary>
    public JsonStorageBackend(
        ILogger<JsonStorageBackend> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _jsonOptions = new Lazy<JsonSerializerOptions>(serviceProvider.GetRequiredService<JsonSerializerOptions>);

        var fileSystem = serviceProvider.GetRequiredService<IFileSystem>();
        _configDirectory = GetConfigsFolderPath(fileSystem);
        _configDirectory.CreateDirectory();
    }

    private static AbsolutePath GetConfigsFolderPath(IFileSystem fileSystem)
    {
        var os = fileSystem.OS;
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_DATA_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );

        // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
        var baseDirectoryName = os.IsOSX ? "NexusMods_App/Configs" : "NexusMods.App/Configs";
        return fileSystem.GetKnownPath(baseKnownPath).Combine(baseDirectoryName);
    }

    private AbsolutePath GetConfigPath<T>()
    {
        var typeName = typeof(T).FullName ?? typeof(T).Name;
        var fileName = $"{typeName}.json";
        return _configDirectory.Combine(fileName);
    }

    private void Serialize<T>(Stream stream ,T value) where T : class
    {
        try
        {
            JsonSerializer.Serialize(stream, value, _jsonOptions.Value);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while serializing `{Type}` with value `{Value}`", typeof(T), value);
        }
    }

    private T? Deserialize<T>(string json) where T : class
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(json, _jsonOptions.Value);
            return value;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Exception while deserializing to `{Type}` with JSON `{Json}`", typeof(T), json);
            return null;
        }
    }

    /// <inheritdoc/>
    public void Save<T>(T value) where T : class, ISettings, new()
    {
        var configPath = GetConfigPath<T>();
        using var stream = configPath.Open(FileMode.Create, FileAccess.ReadWrite, FileShare.None);
        Serialize(stream, value);
    }

    /// <inheritdoc/>
    public T? Load<T>() where T : class, ISettings, new()
    {
        var configPath = GetConfigPath<T>();
        if (!configPath.FileExists) return null;

        using var stream = configPath.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        using var sr = new StreamReader(stream, Encoding.UTF8);

        var json = sr.ReadToEnd();
        return Deserialize<T>(json);
    }

    /// <inheritdoc/>
    public void Dispose() {}
}
