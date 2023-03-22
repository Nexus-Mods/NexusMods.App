using JetBrains.Annotations;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;

namespace NexusMods.DataModel;

/// <summary>
/// Settings for the data model.
/// </summary>
[PublicAPI]
public interface IDataModelSettings
{
    /// <summary>
    /// Path of the file which contains the backing data store or database.
    /// </summary>
    public string DataStoreFilePath { get; }

    /// <summary>
    /// Path of the file which contains the backing data store or database
    /// used for inter process communication.
    /// </summary>
    public string IpcDataStoreFilePath { get; }

    /// <summary>
    /// Preconfigured locations [full paths] where mod archives can/will be stored.
    /// </summary>
    public string[] ArchiveLocations { get; }

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
    private static AbsolutePath DefaultBaseFolder => KnownFolders.EntryFolder;

    /// <inheritdoc />
    public string DataStoreFilePath { get; set; }

    /// <inheritdoc />
    public string IpcDataStoreFilePath { get; set; }

    /// <inheritdoc />
    public string[] ArchiveLocations { get; set; }

    /// <inheritdoc />
    public int MaxHashingJobs { get; set; } = Environment.ProcessorCount;

    /// <inheritdoc />
    public int LoadoutDeploymentJobs { get; set; } = Environment.ProcessorCount;

    /// <inheritdoc />
    public long MaxHashingThroughputBytesPerSecond { get; set; } = 0;

    /// <summary>
    /// Creates the default datamodel settings with a given base directory.
    /// </summary>
    public DataModelSettings() : this(DefaultBaseFolder) { }

    /// <summary>
    /// Creates the default datamodel settings with a given base directory.
    /// </summary>
    /// <param name="baseDirectory">The base directory to use.</param>
    public DataModelSettings(AbsolutePath baseDirectory)
    {
        FileSystem.Shared.CreateDirectory(baseDirectory);
        DataStoreFilePath = baseDirectory.CombineUnchecked(DataModelFileName).GetFullPath();
        IpcDataStoreFilePath = baseDirectory.CombineUnchecked(DataModelIpcFileName).GetFullPath();
        ArchiveLocations = new[]
        {
            baseDirectory.CombineUnchecked(ArchivesFileName).GetFullPath()
        };
    }

    /// <summary>
    /// Expands any user provided paths; and ensures default settings in case of placeholders.
    /// </summary>
    public void Sanitize()
    {
        DataStoreFilePath = KnownFolders.ExpandPath(DataStoreFilePath);
        IpcDataStoreFilePath = KnownFolders.ExpandPath(IpcDataStoreFilePath);
        for (var x = 0; x < ArchiveLocations.Length; x++)
            ArchiveLocations[x] = KnownFolders.ExpandPath(ArchiveLocations[x]);

        MaxHashingJobs = MaxHashingJobs < 0 ? Environment.ProcessorCount : MaxHashingJobs;
        LoadoutDeploymentJobs = LoadoutDeploymentJobs < 0 ? Environment.ProcessorCount : LoadoutDeploymentJobs;
        MaxHashingThroughputBytesPerSecond = MaxHashingThroughputBytesPerSecond <= 0 ? 0 : MaxHashingThroughputBytesPerSecond;

        // Deduplicate: This is necessary in case user has duplicates, or the MSFT configuration
        //              binder inserts a duplicate.
        ArchiveLocations = ArchiveLocations.Distinct().ToArray();
    }
}
