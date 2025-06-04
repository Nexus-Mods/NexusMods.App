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
    private const string DataModelFolderName = "DataModel";
    
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
            .ConfigureStorageBackend<DataModelSettings>(builder => builder.UseJson())
            .AddToUI<DataModelSettings>(builder => builder
                .AddPropertyToUI(x => x.ArchiveLocations, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.General)
                    .WithDisplayName("Downloaded Mods Location")
                    .WithDescription("The location where downloaded mods and archived files are stored.")
                    .UseConfigurablePathsContainer()));
    }

    /// <summary>
    /// Create default value.
    /// </summary>
    public static DataModelSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var os = serviceProvider.GetRequiredService<IFileSystem>().OS;
        var baseKnownPath = GetLocalApplicationDataDirectory(os, out var baseDirectoryName);

        return new DataModelSettings
        {
            MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/{DataModelFolderName}/MnemonicDB.rocksdb"),
            ArchiveLocations = [
                new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/{DataModelFolderName}/Archives"),
            ],
        };
    }

    /// <summary>
    /// Retrieves the base directory where the App stores its local application data.
    /// </summary>
    /// <returns>The absolute path to the local application data directory.</returns>
    public static AbsolutePath GetLocalApplicationDataDirectory(IFileSystem fs)
    {
        var basePath = GetLocalApplicationDataDirectory(fs.OS, out var relativePath);
        return fs.GetKnownPath(basePath).Combine(relativePath);
    }

    /// <summary>
    /// Retrieves the default DataModel folder.
    /// This folder is reserved for the App and should not store user info.
    /// </summary>
    public static AbsolutePath GetStandardDataModelFolder(IFileSystem fs)
    {
        var os = fs.OS;
        var baseKnownPath = GetLocalApplicationDataDirectory(os, out var baseDirectoryName);
        return fs.GetKnownPath(baseKnownPath).Combine(baseDirectoryName).Combine(DataModelFolderName);
    }

    /// <summary>
    /// Retrieves the base directory where the App stores its local application data.
    /// </summary>
    /// <param name="os">OS Information.</param>
    /// <param name="baseDirectoryName">Relative path to the returned <see cref="KnownPath"/>.</param>
    /// <returns></returns>
    private static KnownPath GetLocalApplicationDataDirectory(IOSInformation os, out string baseDirectoryName)
    {
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_DATA_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );

        // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
        baseDirectoryName = os.IsOSX ? "NexusMods_App" : "NexusMods.App";
        return baseKnownPath;
    }
}
