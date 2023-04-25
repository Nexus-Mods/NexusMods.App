using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.App;

/// <summary>
/// The configuration for the Nexus Mods App.
/// </summary>
public class AppConfig
{
    private readonly IFileSystem _fileSystem;

    public AppConfig()
    {
        _fileSystem = FileSystem.Shared;
        DataModelSettings = new DataModelSettings(_fileSystem);
        FileExtractorSettings = new FileExtractorSettings(_fileSystem);
        LoggingSettings = new LoggingSettings(_fileSystem);
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
    /// <param name="baseDirectory">The base directory to use.</param>
    public LoggingSettings(IFileSystem fileSystem)
    {
        var baseFolder = fileSystem.GetKnownPath(KnownPath.EntryDirectory);
        baseFolder.CreateDirectory();
        FilePath = new ConfigurationPath(baseFolder.CombineUnchecked(LogFileName));
        ArchiveFilePath = new ConfigurationPath(baseFolder.CombineUnchecked(LogFileNameTemplate));
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
