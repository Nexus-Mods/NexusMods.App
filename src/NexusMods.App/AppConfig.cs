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
    public DataModelSettings DataModelSettings { get; set; } = new();
    public FileExtractorSettings FileExtractorSettings { get; set; } = new();
    public HttpDownloaderSettings HttpDownloaderSettings { get; set; } = new();
    public LoggingSettings LoggingSettings { get; set; } = new();
}

public interface ILoggingSettings
{
    /// <summary>
    /// Path where the log files will be saved to.
    /// </summary>
    public string FilePath { get; }

    /// <summary>
    /// Path to historical log files, with templated element e.g. 'PATH_TO_FILE/nexusmods.app.{##}.log'
    /// </summary>
    public string ArchiveFilePath { get; }

    /// <summary>
    /// Number of previous log files to store.
    /// </summary>
    public int MaxArchivedFiles { get; }
}

public class LoggingSettings : ILoggingSettings
{
    private const string LogFileName = "nexusmods.app.current.log";
    private const string LogFileNameTemplate = "nexusmods.app.{##}.log";
    private static AbsolutePath DefaultBaseFolder => KnownFolders.EntryFolder;

    /// <inheritdoc/>
    public string FilePath { get; }

    /// <inheritdoc/>
    public string ArchiveFilePath { get; }

    /// <inheritdoc/>
    public int MaxArchivedFiles { get; }

    /// <summary>
    /// Creates the default datamodel settings with a given base directory.
    /// </summary>
    public LoggingSettings() : this(DefaultBaseFolder) { }

    /// <summary>
    /// Creates the default logger settings with a specified base directory to store the log files in.
    /// </summary>
    /// <param name="baseDirectory">The base directory to use.</param>
    public LoggingSettings(AbsolutePath baseDirectory)
    {
        FileSystem.Shared.CreateDirectory(baseDirectory);
        FilePath = baseDirectory.CombineUnchecked(LogFileName).GetFullPath();
        ArchiveFilePath = baseDirectory.CombineUnchecked(LogFileNameTemplate).GetFullPath();
        MaxArchivedFiles = 10;
    }
}
