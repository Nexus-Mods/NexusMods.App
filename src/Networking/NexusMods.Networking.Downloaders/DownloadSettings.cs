using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.Networking.Downloaders;

/// <summary>
/// Settings for downloads.
/// </summary>
[PublicAPI]
public record DownloadSettings : ISettings
{
    
    /// <summary>
    /// Base directory where files generated during ongoing download operations are located.
    /// </summary>
    /// <remarks>
    /// Should not be placed in Temp or similar directories,
    /// as download files may need to persist across application and system restarts.
    /// </remarks>
    public ConfigurablePath OngoingDownloadLocation { get; set; }
    
    
    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureDefault(CreateDefault)
            .ConfigureStorageBackend<DownloadSettings>(builder => builder.UseJson());
    }
    
    /// <summary>
    /// Create default value.
    /// </summary>
    public static DownloadSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var os = serviceProvider.GetRequiredService<IFileSystem>().OS;

        return new DownloadSettings
        {
            OngoingDownloadLocation = GetStandardDownloadPath(os),
        };
    }
    
    private static ConfigurablePath GetStandardDownloadPath(IOSInformation os)
    {
         var basePath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_DATA_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );
        // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
        var baseDirectoryName = os.IsOSX ? "NexusMods_App/" : "NexusMods.App/";
        var downloadsSubPath = baseDirectoryName + "Downloads/Ongoing";
        
        return new ConfigurablePath(basePath, downloadsSubPath);
    }

    /// <summary>
    /// Absolute path to the standard downloads' folder.
    /// </summary>
    public static AbsolutePath GetStandardDownloadsFolder(IFileSystem fs)
    { 
        return GetStandardDownloadPath(fs.OS).ToPath(fs);
    }
}
