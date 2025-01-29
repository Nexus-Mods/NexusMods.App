using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.Games.FileHashes;

[PublicAPI]
public record FileHashesServiceSettings : ISettings
{
    /// <summary>
    /// Location where the temporary folder will be stored.
    /// </summary>
    public ConfigurablePath HashDatabaseLocation { get; init; }
    
    /// <summary>
    /// Only checks GitHub for updates this often, in order to avoid API rate limits.
    /// </summary>
    public TimeSpan HashDatabaseUpdateInterval { get; init; } = TimeSpan.FromHours(6);
    
    /// <summary>
    /// The URL to the Github API to get the latest release.
    /// </summary>
    public Uri GithubManifestUrl { get; init; } = new("https://github.com/Nexus-Mods/game-hashes/releases/latest/download/manifest.json");
    
    /// <summary>
    /// The URL to the GitHub release to download the latest hashes database.
    /// </summary>
    public Uri GameHashesDbUrl { get; init; } = new("https://github.com/Nexus-Mods/game-hashes/releases/latest/download/game_hashes_db.zip");
    
    public RelativePath ReleaseFilePath { get; init; } = "game-hashes-release.json";
    

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureDefault(CreateDefault)
            .ConfigureStorageBackend<FileHashesServiceSettings>(builder => builder.Disable());
    }

    /// <summary>
    /// Create default.
    /// </summary>
    public static FileHashesServiceSettings CreateDefault(IServiceProvider serviceProvider)
    {
        var os = serviceProvider.GetRequiredService<IFileSystem>().OS;

        // Note: The idiomatic place for this is Temporary Directory (/tmp on Linux, %TEMP% on Windows)
        //       however this can be dangerous to do on Linux, as /tmp is often a RAM disk, and can be
        //       too small to handle large files.
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_DATA_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );

        return new FileHashesServiceSettings
        {
            HashDatabaseLocation = new ConfigurablePath(baseKnownPath, "NexusMods.App/FileHashesDatabase"),
        };
    }
}
