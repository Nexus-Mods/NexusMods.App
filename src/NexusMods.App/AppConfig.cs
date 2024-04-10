using NexusMods.Abstractions.FileExtractor;
using NexusMods.Abstractions.HttpDownloader;
using NexusMods.App.UI;
using NexusMods.DataModel;
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
    public void Sanitize(IFileSystem fs)
    {
        DataModelSettings.Sanitize(fs);
        FileExtractorSettings.Sanitize(fs);
        HttpDownloaderSettings.Sanitize();
        LoggingSettings.Sanitize(fs);
    }
}

public interface ILoggingSettings
{
    /// <summary>
    /// Gets the path to the current log file for the main process.
    /// </summary>
    public ConfigurationPath MainProcessLogFilePath { get; }

    /// <summary>
    /// Gets the template path to the archive log file for the main process.
    /// </summary>
    /// <remarks>
    /// For details, see https://nlog-project.org/documentation/v5.0.0/html/P_NLog_Targets_FileTarget_ArchiveFileName.htm
    /// </remarks>
    public ConfigurationPath MainProcessArchiveFilePath { get; }

    /// <summary>
    /// Gets the path to the current log file for the slim process.
    /// </summary>
    public ConfigurationPath SlimProcessLogFilePath { get; }

    /// <summary>
    /// Gets the template path to the archive log file for the slim process.
    /// </summary>
    /// <remarks>
    /// For details, see https://nlog-project.org/documentation/v5.0.0/html/P_NLog_Targets_FileTarget_ArchiveFileName.htm
    /// </remarks>
    public ConfigurationPath SlimProcessArchiveFilePath { get; }

    /// <summary>
    /// Number of previous log files to store.
    /// </summary>
    public int MaxArchivedFiles { get; }
}

public class LoggingSettings : ILoggingSettings
{
    /// <inheritdoc/>
    public ConfigurationPath MainProcessLogFilePath { get; private set; }

    /// <inheritdoc/>
    public ConfigurationPath MainProcessArchiveFilePath { get; private set; }
    /// <inheritdoc/>
    public ConfigurationPath SlimProcessLogFilePath { get; private set; }

    /// <inheritdoc/>
    public ConfigurationPath SlimProcessArchiveFilePath { get; private set; }

    /// <inheritdoc/>
    public int MaxArchivedFiles { get; set; }

    /// <summary>
    /// Default constructor for serialization.
    /// </summary>
    public LoggingSettings() : this(FileSystem.Shared) { }

    /// <summary>
    /// Creates the default logger with logs stored in the entry directory.
    /// </summary>
    /// <param name="fileSystem">The FileSystem implementation to use.</param>
    public LoggingSettings(IFileSystem fileSystem)
    {
        var baseFolder = GetDefaultBaseDirectory(fileSystem);
        MainProcessLogFilePath = GetFilePath(baseFolder, forMainProcess: true);
        MainProcessArchiveFilePath = GetArchiveFilePath(baseFolder, forMainProcess: true);
        SlimProcessLogFilePath = GetFilePath(baseFolder, forMainProcess: false);
        SlimProcessArchiveFilePath = GetArchiveFilePath(baseFolder, forMainProcess: false);
        MaxArchivedFiles = 10;
    }

    /// <summary>
    /// Expands any user provided paths; and ensures default settings in case of placeholders.
    /// </summary>
    public void Sanitize(IFileSystem fs)
    {
        MaxArchivedFiles = MaxArchivedFiles < 0 ? 10 : MaxArchivedFiles;

        // Set default locations if none are provided.
        var baseFolder = GetDefaultBaseDirectory(fs);
        if (string.IsNullOrEmpty(MainProcessLogFilePath.RawPath))
        {
            MainProcessLogFilePath = GetFilePath(baseFolder, forMainProcess: true);
        }

        if (string.IsNullOrEmpty(MainProcessArchiveFilePath.RawPath))
        {
            MainProcessArchiveFilePath = GetArchiveFilePath(baseFolder, forMainProcess: true);
        }

        if (string.IsNullOrEmpty(SlimProcessLogFilePath.RawPath))
        {
            SlimProcessLogFilePath = GetFilePath(baseFolder, forMainProcess: false);
        }

        if (string.IsNullOrEmpty(SlimProcessArchiveFilePath.RawPath))
        {
            SlimProcessArchiveFilePath = GetArchiveFilePath(baseFolder, forMainProcess: false);
        }

        MainProcessLogFilePath.ToAbsolutePath().Parent.CreateDirectory();
        MainProcessArchiveFilePath.ToAbsolutePath().Parent.CreateDirectory();
        SlimProcessLogFilePath.ToAbsolutePath().Parent.CreateDirectory();
        SlimProcessArchiveFilePath.ToAbsolutePath().Parent.CreateDirectory();
    }

    private static AbsolutePath GetDefaultBaseDirectory(IFileSystem fs)
    {
        return fs.OS.MatchPlatform(
            () => fs.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("NexusMods.App/Logs"),
            () => fs.GetKnownPath(KnownPath.XDG_STATE_HOME).Combine("NexusMods.App/Logs"),
            // Using _ instead of . So OSX doesn't think that the folder is an app 🙄
            () => fs.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("NexusMods_App/Logs")
        );
    }


    private static ConfigurationPath GetFilePath(AbsolutePath baseFolder, bool forMainProcess)
    {
        var logFileName = forMainProcess
            ? "nexusmods.app.main.current.log"
            : "nexusmods.app.slim.current.log";

        return new ConfigurationPath(baseFolder.Combine(logFileName));
    }

    private static ConfigurationPath GetArchiveFilePath(AbsolutePath baseFolder, bool forMainProcess)
    {
        var logFileNameTemplate = forMainProcess
            ? "nexusmods.app.main.{##}.log"
            : "nexusmods.app.slim.{##}.log";
        return new ConfigurationPath(baseFolder.Combine(logFileNameTemplate));
    }
}
