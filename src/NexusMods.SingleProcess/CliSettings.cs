using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.SingleProcess;

/// <summary>
/// Settings for the CLI server.
/// </summary>
public class CliSettings() : ISettings
{
    /// <inheritdoc />
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        var sectionId = Sections.DeveloperTools;
        return settingsBuilder
                .ConfigureDefault(CreateDefault)
                .ConfigureStorageBackend<CliSettings>(builder => builder.UseJson())
                .AddToUI<CliSettings>(builder => builder
                    .AddPropertyToUI(x => x.StartCliBackend, propertyBuilder => propertyBuilder
                        .AddToSection(sectionId)
                        .WithDisplayName("Start CLI Backend")
                        .WithDescription("On application start, opens a localhost TCP connection that accepts CLI commands.")
                        .UseBooleanContainer()
                        .RequiresRestart()
                    ));
    }

    /// <summary>
    /// Default constructor for the settings
    /// </summary>
    /// <param name="provider"></param>
    /// <returns></returns>
    private static CliSettings CreateDefault(IServiceProvider provider)
    {
        var fs = provider.GetRequiredService<IFileSystem>();
        var directory = fs.OS.MatchPlatform(
            () => fs.GetKnownPath(KnownPath.LocalApplicationDataDirectory).Combine("NexusMods.App"),
            () => fs.GetKnownPath(KnownPath.XDG_RUNTIME_DIR),
            () => fs.GetKnownPath(KnownPath.ApplicationDataDirectory).Combine("NexusMods_App")
        );

        return new CliSettings
        {
            SyncFile = directory.Combine("NexusMods.App-sync_file.sync"),
        };
    }

    /// <summary>
    /// If true the CLI backend will be started, otherwise it will not be started, and CLI commands will not be available.
    /// </summary>
    public bool StartCliBackend { get; set; } = true;

    /// <summary>
    /// The path to the sync file, this file is used to publish the process id of the main process, and the TCP port it's listening on.
    /// </summary>
    public AbsolutePath SyncFile { get; set; } = default!;
    
}
