using System.Text.Json;
using NexusMods.App.UI;
using NexusMods.DataModel;
using NexusMods.DataModel.GlobalSettings;
using NexusMods.FileExtractor;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;

namespace NexusMods.App;

/// <summary>
/// The configuration for the Nexus Mods App.
/// </summary>
public class AppConfig
{
    public AppConfig()
    {
        var fileSystem = FileSystem.Shared;
        DataModelSettings = new DataModelSettings(fileSystem);
        FileExtractorSettings = new FileExtractorSettings(fileSystem);
        LoggingSettings = new LoggingSettings(fileSystem);
    }
    /*
        Default Value Rules:

        - A value of < 0 for a positive value assumes 'use default value' where appropriate.
            - e.g. 'Maximum Throughput' of -1, means unlimited.
            - e.g. 'Simultaneous Jobs' of -1, means auto determine.
            - If a value of 0 is invalid for specific field, also use default.
            - e.g. 'Simultaneous Jobs' of 0, means auto determine.

        - Paths can be expanded using the following etc.
            - {EntryFolder} : Directory this program's DLL resolver uses to probe for DLLs.
            - {CurrentDirectory} : Current Directory/PWD.
            - {HomeFolder} : User's home folder.

            - These monikers are case insensitive, and implemented inside paths library.

        - Arrays must not contain duplicates.
            - This can either happen due to user error or the Microsoft Config Binder injecting
              a duplicate into the array.

        Individual settings objects must implement a `Sanitize` function to ensure these rules.
    */

    public DataModelSettings DataModelSettings { get; set; }
    public FileExtractorSettings FileExtractorSettings { get; set; }
    public HttpDownloaderSettings HttpDownloaderSettings { get; set; } = new();
    public LoggingSettings LoggingSettings { get; set; }
    public LauncherSettings LauncherSettings { get; set; } = new();
    public bool? EnableTelemetry { get; set; }

    /// <summary>
    /// Sanitizes the config; e.g.
    /// </summary>
    public void Sanitize()
    {
        DataModelSettings.Sanitize();
        HttpDownloaderSettings.Sanitize();
        LoggingSettings.Sanitize();
    }
}

public interface ILoggingSettings
{
    /// <summary>
    /// Path where the log files will be saved to.
    /// </summary>
    public ConfigurationPath FilePath { get; }

    /// <summary>
    /// Path to historical log files, with templated element e.g. 'PATH_TO_FILE/nexusmods.app.{##}.log'
    /// </summary>
    public ConfigurationPath ArchiveFilePath { get; }

    /// <summary>
    /// Number of previous log files to store.
    /// </summary>
    public int MaxArchivedFiles { get; }
}

public class LoggingSettings : ILoggingSettings
{
    private const string LogFileName = "nexusmods.app.current.log";
    private const string LogFileNameTemplate = "nexusmods.app.{##}.log";

    /// <inheritdoc/>
    public ConfigurationPath FilePath { get; set; }

    /// <inheritdoc/>
    public ConfigurationPath ArchiveFilePath { get; set; }

    /// <inheritdoc/>
    public int MaxArchivedFiles { get; set; }

    /// <summary>
    /// Default constructor for serialization.
    /// </summary>
    public LoggingSettings() : this(FileSystem.Shared) {}

    /// <summary>
    /// Creates the default logger with logs stored in the entry directory.
    /// </summary>
    /// <param name="fileSystem">The FileSystem implementation to use.</param>
    public LoggingSettings(IFileSystem fileSystem)
    {
        var baseFolder = fileSystem.GetKnownPath(KnownPath.EntryDirectory);
        baseFolder.CreateDirectory();
        FilePath = new ConfigurationPath(baseFolder.Combine(LogFileName));
        ArchiveFilePath = new ConfigurationPath(baseFolder.Combine(LogFileNameTemplate));
        MaxArchivedFiles = 10;
    }

    /// <summary>
    /// Expands any user provided paths; and ensures default settings in case of placeholders.
    /// </summary>
    public void Sanitize()
    {
        MaxArchivedFiles = MaxArchivedFiles < 0 ? 10 : MaxArchivedFiles;
    }
}

internal class AppConfigManager : IAppConfigManager
{
    private readonly AppConfig _config;
    private readonly AbsolutePath _configPath;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public AppConfigManager(AppConfig config, JsonSerializerOptions jsonSerializerOptions)
    {
        _config = config;
        _configPath = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("AppConfig.json");
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public bool GetMetricsOptIn() => _config.EnableTelemetry ?? false;

    public void SetMetricsOptIn(bool value)
    {
        _config.EnableTelemetry = value;
        var res = JsonSerializer.SerializeToUtf8Bytes(_config, _jsonSerializerOptions);
        _configPath.WriteAllBytesAsync(res);
    }

    public bool IsMetricsOptInSet() => _config.EnableTelemetry.HasValue;
}
