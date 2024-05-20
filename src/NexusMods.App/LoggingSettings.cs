using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;

namespace NexusMods.App;

public record LoggingSettings : ISettings
{
    /// <summary>
    /// Gets the path to the current log file for the main process.
    /// </summary>
    public ConfigurablePath MainProcessLogFilePath { get; init; }

    /// <summary>
    /// Gets the template path to the archive log file for the main process.
    /// </summary>
    /// <remarks>
    /// For details, see https://nlog-project.org/documentation/v5.0.0/html/P_NLog_Targets_FileTarget_ArchiveFileName.htm
    /// </remarks>
    public ConfigurablePath MainProcessArchiveFilePath { get; init; }

    /// <summary>
    /// Gets the path to the current log file for the slim process.
    /// </summary>
    public ConfigurablePath SlimProcessLogFilePath { get; init; }

    /// <summary>
    /// Gets the path to the current log file for the slim process.
    /// </summary>
    public ConfigurablePath SlimProcessArchiveFilePath { get; init; }

    /// <summary>
    /// Number of previous log files to store.
    /// </summary>
    public int MaxArchivedFiles { get; init; } = 10;

    /// <summary>
    /// Gets the minimum logging level.
    /// </summary>
    public LogLevel MinimumLevel { get; init; } = LogLevel.Debug;

    /// <summary>
    /// When enabled, logs will be written to the console as well as the log file.
    /// </summary>
    #if DEBUG
    public bool LogToConsole { get; init; } = true;
    #else
    public bool LogToConsole { get; init; } = false;
    #endif

    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        // TODO: put in some section
        var sectionId = SectionId.DefaultValue;

        return settingsBuilder
            .ConfigureDefault(serviceProvider => CreateDefault(serviceProvider.GetRequiredService<IFileSystem>().OS))
            .ConfigureStorageBackend<LoggingSettings>(builder => builder.UseJson())
            .AddToUI<LoggingSettings>(builder => builder
                .AddPropertyToUI(x => x.MinimumLevel, propertyBuilder => propertyBuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("Minimum Logging Level")
                    .WithDescription("Set the minimum logging level. Recommended: Debug")
                    .UseSingleValueMultipleChoiceContainer(
                        valueComparer: EqualityComparer<LogLevel>.Default,
                        allowedValues: [
                            LogLevel.Information,
                            LogLevel.Debug,
                            LogLevel.Trace,
                        ],
                        valueToDisplayString: static logLevel => logLevel.ToString()
                    )
                    .RequiresRestart()
                )
                .AddPropertyToUI(x => x.LogToConsole, propertybuilder => propertybuilder
                    .AddToSection(sectionId)
                    .WithDisplayName("Log to Console")
                    .WithDescription("When enabled, logs will be written to the console as well as the log file.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
            );
    }

    public static LoggingSettings CreateDefault(IOSInformation os)
    {
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_STATE_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );

        // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
        var baseDirectoryName = os.IsOSX ? "NexusMods_App/Logs" : "NexusMods.App/Logs";

        return new LoggingSettings
        {
            MainProcessLogFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.main.current.log"),
            MainProcessArchiveFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.main.{{##}}.log"),
            SlimProcessLogFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.slim.current.log"),
            SlimProcessArchiveFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.slim.{{##}}.log"),
        };
    }
}
