using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;

namespace NexusMods.Sdk.FileExtractor;

/// <summary>
/// Settings for the file extractor.
/// </summary>
[PublicAPI]
public record FileExtractorSettings : ISettings
{
    /// <summary>
    /// Location where the temporary folder will be stored.
    /// </summary>
    public ConfigurablePath TempFolderLocation { get; init; }

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureDefault(CreateDefault)
            .ConfigureBackend(StorageBackendOptions.Disable);
    }

    /// <summary>
    /// Create default.
    /// </summary>
    public static FileExtractorSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var os = serviceProvider.GetRequiredService<IFileSystem>().OS;

        // Note: The idiomatic place for this is Temporary Directory (/tmp on Linux, %TEMP% on Windows)
        //       however this can be dangerous to do on Linux, as /tmp is often a RAM disk, and can be
        //       too small to handle large files.
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.TempDirectory,
            onLinux: () => KnownPath.XDG_STATE_HOME,
            onOSX: () => KnownPath.TempDirectory
        );

        return new FileExtractorSettings
        {
            TempFolderLocation = new ConfigurablePath(baseKnownPath, "NexusMods.App/Temp"),
        };
    }
}
