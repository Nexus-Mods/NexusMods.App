using JetBrains.Annotations;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Settings for the data model.
/// </summary>
[PublicAPI]
public interface IDataModelSettings
{
    /// <summary>
    /// If true, data model will be stored in memory only and the paths will be ignored.
    /// </summary>
    public bool UseInMemoryDataModel { get; }

    /// <summary>
    /// Path of the file which contains the backing data store or database.
    /// </summary>
    public ConfigurationPath DataStoreFilePath { get; }

    /// <summary>
    /// Path of the file which contains the backing data store or database
    /// used for inter process communication.
    /// </summary>
    public ConfigurationPath IpcDataStoreFilePath { get; }

    /// <summary>
    /// Preconfigured locations [full paths] where mod archives can/will be stored.
    /// </summary>
    public ConfigurationPath[] ArchiveLocations { get; }

    /// <summary>
    /// Maximum number of simultaneous hashing jobs.
    /// Each job basically corresponds to CPU core.
    /// </summary>
    public int MaxHashingJobs { get; }

    /// <summary>
    /// Maximum number of simultaneous loadout deployment jobs.
    /// Each job basically corresponds to CPU core.
    /// </summary>
    public int LoadoutDeploymentJobs { get; }

    /// <summary>
    /// Maximum throughput for the hashing operations in bytes per second.
    /// Value of 0 represents no cap.
    /// </summary>
    public long MaxHashingThroughputBytesPerSecond { get; }
}

/// <summary>
/// Default implementation of <see cref="IDataModelSettings"/> for reference.
/// </summary>
[PublicAPI]
public class DataModelSettings : IDataModelSettings
{
    // Note: We can't serialize AbsolutePath because it contains more fields than expected. Just hope user sets correct paths and pray for the best.

    private const string DataModelFileName = "DataModel.sqlite";
    private const string DataModelIpcFileName = "DataModel_IPC.sqlite";
    private const string ArchivesFileName = "Archives";

    /// <inheritdoc />
    public bool UseInMemoryDataModel { get; set; }

    /// <inheritdoc />
    public ConfigurationPath DataStoreFilePath { get; set; }

    /// <inheritdoc />
    public ConfigurationPath IpcDataStoreFilePath { get; set; }

    /// <inheritdoc />
    public ConfigurationPath[] ArchiveLocations { get; set; }

    /// <inheritdoc />
    public int MaxHashingJobs { get; set; } = Environment.ProcessorCount;

    /// <inheritdoc />
    public int LoadoutDeploymentJobs { get; set; } = Environment.ProcessorCount;

    /// <inheritdoc />
    public long MaxHashingThroughputBytesPerSecond { get; set; } = 0;

    /// <summary>
    /// Default constructor for serialization.
    /// </summary>
    public DataModelSettings() : this(FileSystem.Shared) { }

    /// <summary>
    /// Creates the default datamodel settings with a given base directory.
    /// </summary>
    public DataModelSettings(IFileSystem s) : this(GetDefaultBaseDirectory(s)) { }

    /// <summary>
    /// Creates the default datamodel settings with a given base directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory to use.</param>
    public DataModelSettings(AbsolutePath baseDirectory)
    {
        DataStoreFilePath = GetDefaultDataStoreFilePath(baseDirectory);
        IpcDataStoreFilePath = GetDefaultIpcFilePath(baseDirectory);
        ArchiveLocations = GetDefaultArchiveLocations(baseDirectory);
    }

    /// <summary>
    /// Ensures default settings in case of placeholders of undefined/invalid settings.
    /// </summary>
    public void Sanitize(IFileSystem fs)
    {
        MaxHashingJobs = MaxHashingJobs < 0 ? Environment.ProcessorCount : MaxHashingJobs;
        LoadoutDeploymentJobs = LoadoutDeploymentJobs < 0 ? Environment.ProcessorCount : LoadoutDeploymentJobs;
        MaxHashingThroughputBytesPerSecond =
            MaxHashingThroughputBytesPerSecond <= 0 ? 0 : MaxHashingThroughputBytesPerSecond;

        // Deduplicate: This is necessary in case user has duplicates, or (in the past) MSFT configuration
        //              binder would insert a duplicate.
        ArchiveLocations = ArchiveLocations.Distinct().Where(x => !string.IsNullOrEmpty(x.GetFullPath())).ToArray();

        // Set default locations if none are provided.
        var baseDir = GetDefaultBaseDirectory(fs);
        if (ArchiveLocations.Length == 0)
            ArchiveLocations = GetDefaultArchiveLocations(baseDir);

        if (string.IsNullOrEmpty(DataStoreFilePath.RawPath))
            DataStoreFilePath = GetDefaultDataStoreFilePath(baseDir);

        if (string.IsNullOrEmpty(IpcDataStoreFilePath.RawPath))
            IpcDataStoreFilePath = GetDefaultIpcFilePath(baseDir);

        // Ensure all locations exist
        foreach (var location in ArchiveLocations)
            location.ToAbsolutePath().CreateDirectory();

        DataStoreFilePath.ToAbsolutePath().Parent.CreateDirectory();
        IpcDataStoreFilePath.ToAbsolutePath().Parent.CreateDirectory();
    }

    private static AbsolutePath GetDefaultBaseDirectory(IFileSystem fs)
    {
        return fs.OS.MatchPlatform(
            () => fs.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("DataModel"),
            () => fs.GetKnownPath(KnownPath.XDG_DATA_HOME).Combine("DataModel"),
            () => throw new NotSupportedException(
                "(Note: Sewer) Paths needs PR for macOS. I don't have a non-painful way to access a Mac."));
    }

    private static ConfigurationPath GetDefaultDataStoreFilePath(AbsolutePath baseDirectory) =>
        new(baseDirectory.Combine(DataModelFileName));

    private static ConfigurationPath GetDefaultIpcFilePath(AbsolutePath baseDirectory) =>
        new(baseDirectory.Combine(DataModelIpcFileName));

    private static ConfigurationPath[] GetDefaultArchiveLocations(AbsolutePath baseDirectory) =>
        new[]
        {
            new ConfigurationPath(baseDirectory.Combine(ArchivesFileName))
        };
}
