using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.DataModel;

/// <summary>
/// Settings for the data model.
/// </summary>
[PublicAPI]
public record DataModelSettings : ISettings
{
    /// <summary>
    /// If true, data model will be stored in memory only and the paths will be ignored.
    /// </summary>
    public bool UseInMemoryDataModel { get; set; }
    
    /// <summary>
    /// Path of the folder containing the MnemonicDB database.
    /// </summary>
    public ConfigurablePath MnemonicDBPath { get; set; }

    /// <summary>
    /// Preconfigured locations [full paths] where mod archives can/will be stored.
    /// </summary>
    public ConfigurablePath[] ArchiveLocations { get; set; } = [];

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureDefault(CreateDefault)
            .ConfigureStorageBackend<DataModelSettings>(builder => builder.UseJson());
    }

    /// <summary>
    /// Create default value.
    /// </summary>
    public static DataModelSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var os = serviceProvider.GetRequiredService<IFileSystem>().OS;
        var baseKnownPath = GetStandardDataModelPaths(os, out var baseDirectoryName);

        return new DataModelSettings
        {
            MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/MnemonicDB.rocksdb"),
            ArchiveLocations = [
                new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/Archives"),
            ],
        };
    }

    private static KnownPath GetStandardDataModelPaths(IOSInformation os, out string baseDirectoryName)
    {
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_DATA_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );

        // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
        baseDirectoryName = os.IsOSX ? "NexusMods_App/DataModel" : "NexusMods.App/DataModel";
        return baseKnownPath;
    }
    
    /// <summary>
    /// Retrieves the default DataModel folder.
    /// This folder is reserved for the App and should not store user info.
    /// </summary>
    public static AbsolutePath GetStandardDataModelFolder(IFileSystem fs)
    {
        var os = fs.OS;
        var baseKnownPath = GetStandardDataModelPaths(os, out var baseDirectoryName);
        return fs.GetKnownPath(baseKnownPath).Combine(baseDirectoryName);
    }
}
