using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.Paths;

namespace NexusMods.CrossPlatform;

/// <summary>
/// Settings related to logging in the Nexus Mods App. 
/// </summary>
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
    public LogLevel MinimumLevel { get; [UsedImplicitly] set; } = LogLevel.Debug;

    /// <summary>
    /// When enabled, logs will be written to the console as well as the log file.
    /// </summary>
    public bool LogToConsole { get; [UsedImplicitly] set; } = CompileConstants.IsDebug;

    /// <summary>
    /// Gets the retention span for process logs.
    /// </summary>
    public TimeSpan ProcessLogRetentionSpan { get; } = TimeSpan.FromDays(7);

    /// <summary>
    /// When enabled, shows an exception modal to the user on every observed exception.
    /// </summary>
    public bool ShowExceptions { get; [UsedImplicitly] set; } = true;

    /// <inheritdoc/>
    public static ISettingsBuilder Configure(ISettingsBuilder settingsBuilder)
    {
        return settingsBuilder
            .ConfigureDefault(serviceProvider => CreateDefault(serviceProvider.GetRequiredService<IFileSystem>().OS))
            .ConfigureStorageBackend<LoggingSettings>(builder => builder.UseJson())
            .AddToUI<LoggingSettings>(builder => builder
                .AddPropertyToUI(x => x.MinimumLevel, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.General)
                    .WithDisplayName("Minimum logging level")
                    .WithDescription("Sets the minimum logging level. Recommended: Debug")
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
                .AddPropertyToUI(x => x.LogToConsole, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.DeveloperTools)
                    .WithDisplayName("Log to console")
                    .WithDescription("Enables the ConsoleTarget (stdout) for all loggers.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
                .AddPropertyToUI(x => x.ShowExceptions, propertyBuilder => propertyBuilder
                    .AddToSection(Sections.DeveloperTools)
                    .WithDisplayName("Show exception modal")
                    .WithDescription("Enables the exception modal.")
                    .UseBooleanContainer()
                    .RequiresRestart()
                )
            );
    }
    
    public static AbsolutePath GetLogBaseFolder(IOSInformation os, IFileSystem fs)
    {
        var baseKnownPath = BaseKnownPath(os);
        var baseDirectoryName = GetBaseDirectoryName(os);
        return fs.GetKnownPath(baseKnownPath).Combine(baseDirectoryName);
    }

    public static LoggingSettings CreateDefault(IOSInformation os)
    {
        var baseKnownPath = BaseKnownPath(os);
        var baseDirectoryName = GetBaseDirectoryName(os);

        return new LoggingSettings
        {
            MainProcessLogFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.main.current.log"),
            MainProcessArchiveFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.main.{{##}}.log"),
            SlimProcessLogFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.slim.current.log"),
            SlimProcessArchiveFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectoryName}/nexusmods.app.slim.{{##}}.log"),
        };
    }
    
    // NOTE: OSX ".App" is apparently special, using _ instead of . to prevent weirdness
    private static string GetBaseDirectoryName(IOSInformation os) => os.IsOSX ? "NexusMods_App/Logs" : "NexusMods.App/Logs";

    private static KnownPath BaseKnownPath(IOSInformation os)
    {
        var baseKnownPath = os.MatchPlatform(
            onWindows: () => KnownPath.LocalApplicationDataDirectory,
            onLinux: () => KnownPath.XDG_STATE_HOME,
            onOSX: () => KnownPath.LocalApplicationDataDirectory
        );
        return baseKnownPath;
    }
}
