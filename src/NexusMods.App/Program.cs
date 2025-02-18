using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.CrossPlatform;
using NexusMods.CrossPlatform.Process;
using NexusMods.DataModel;
using NexusMods.DataModel.SchemaVersions;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.Settings;
using NexusMods.SingleProcess;
using NexusMods.SingleProcess.Exceptions;
using NexusMods.StandardGameLocators;
using NLog.Extensions.Logging;
using NLog.Targets;
using ReactiveUI;
using Spectre.Console;

namespace NexusMods.App;

public class Program
{
    private static ILogger<Program> _logger = default!;

    [STAThread]
    public static int Main(string[] args)
    {
        // This code will not work properly if it comes after anything that uses the console. So we need to do this first.
        if (OperatingSystem.IsWindows())
            ConsoleHelper.EnsureConsole();
        
        MainThreadData.SetMainThread();

        TelemetrySettings telemetrySettings;
        LoggingSettings loggingSettings;
        ExperimentalSettings experimentalSettings;
        GameLocatorSettings gameLocatorSettings;
        using (var settingsHost = BuildSettingsHost())
        {
            var settingsManager = settingsHost.Services.GetRequiredService<ISettingsManager>();
            telemetrySettings = settingsManager.Get<TelemetrySettings>();
            loggingSettings = settingsManager.Get<LoggingSettings>();
            experimentalSettings = settingsManager.Get<ExperimentalSettings>();
            gameLocatorSettings = settingsManager.Get<GameLocatorSettings>();
        }

        var startupMode = StartupMode.Parse(args);
        
        using var host = BuildHost(
            startupMode,
            telemetrySettings,
            loggingSettings,
            experimentalSettings,
            gameLocatorSettings
        );
        var services = host.Services;

        // Okay to do wait here, as we are in the main process thread.
        host.StartAsync().Wait(timeout: TimeSpan.FromMinutes(5));
        
        if (startupMode.RunAsMain)
        {
            var dataModelSettings = services.GetRequiredService<ISettingsManager>().Get<DataModelSettings>();
            var fileSystem = services.GetRequiredService<IFileSystem>();

            var modelExists = dataModelSettings.MnemonicDBPath.ToPath(fileSystem).DirectoryExists();
            
            // This will startup the MnemonicDb connection
            var migration = services.GetRequiredService<MigrationService>();
            if (modelExists)
            {
                // Run the migrations
                migration.MigrateAll().Wait();
            }
            else
            {
                // Otherwise, perform the initial setup
                migration.InitialSetup().Wait();
            }
        }


        // Start the CLI server if we are the main process.
        var cliServer = services.GetService<CliServer>();
        cliServer?.StartCliServerAsync().Wait(timeout: TimeSpan.FromSeconds(5));

        _logger = services.GetRequiredService<ILogger<Program>>();
        LogMessages.RuntimeInformation(_logger, RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, CompileConstants.InstallationMethod);
        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            LogMessages.UnobservedTaskException(_logger, eventArgs.Exception, sender, sender?.GetType());
            eventArgs.SetObserved();
        };

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            LogMessages.UnobservedReactiveThrownException(_logger, ex);
        });


        try
        {
            if (startupMode.RunAsMain)
            {
                LogMessages.StartingProcess(_logger, Environment.ProcessPath, Environment.ProcessId, args);

                if (startupMode.ShowUI)
                {
                    var task = RunCliTaskAsMain(services, startupMode);
                    Startup.Main(services, []);
                    return task.Result;
                }
                else
                {
                    var task = RunCliTaskAsMain(services, startupMode);
                    return task.Result;
                }
            }
            else
            {
                var task = RunCliTaskRemotely(services, startupMode);
                return task.Result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            return 1;
        }
        finally
        {
            host.StopAsync().Wait(timeout: TimeSpan.FromSeconds(5));
        }
    }

    private static async Task<int> RunCliTaskRemotely(IServiceProvider services, StartupMode startupMode)
    {
        var client = services.GetRequiredService<CliClient>();
        var syncFile = services.GetRequiredService<SyncFile>();
        try
        {
            await client.ExecuteCommand(startupMode.Args, AnsiConsole.Console);
            return 0;
        }
        catch (NoMainProcessStarted _)
        {
            var interop = services.GetRequiredService<IOSInterop>();
            var ownExe = interop.GetOwnExe();
            _logger.LogInformation("No main process started, starting {OwnExe}", ownExe);
            var processInfo = new ProcessStartInfo
            {
                FileName = ownExe.ToString(),
                Arguments = "",
                UseShellExecute = true,
            };
            var process = Process.Start(processInfo);
            if (process is null)
            {
                _logger.LogError("Failed to start main process {OwnExe}", ownExe);
                return 1;
            }
            else
            {
                _logger.LogInformation("Started main process {OwnExe} with id {ProcessId}", ownExe, process.Id);
            }

            var st = Stopwatch.StartNew();
            while (!syncFile.IsMainRunning)
            {
                await Task.Delay(100);
                if (st.Elapsed > TimeSpan.FromSeconds(60))
                {
                    _logger.LogError("Main process {OwnExe} did not start", ownExe);
                    return 1;
                }
            }
                
            await client.ExecuteCommand(startupMode.Args, AnsiConsole.Console);
            return 0;
        }
    }

    /// <summary>
    /// Runs the CLI task in this process. 
    /// </summary>
    private static Task<int> RunCliTaskAsMain(IServiceProvider provider, StartupMode startupMode)
    {
        if (!startupMode.ExecuteCli)
            return Task.FromResult(0);
        var configurator = provider.GetRequiredService<CommandLineConfigurator>();
        _logger.LogInformation("Starting with Spectre.Cli");
        return configurator.RunAsync(startupMode.Args, new SpectreRenderer(AnsiConsole.Console), CancellationToken.None);
    }

    private static IHost BuildSettingsHost()
    {
        var host = new HostBuilder()
            .ConfigureServices(services => services
                .AddSingleton(FileSystem.Shared)
                .AddSettingsManager()
                .AddSerializationAbstractions()
                .AddSettingsStorageBackend<JsonStorageBackend>()
                .AddSettings<TelemetrySettings>()
                .AddSettings<LoggingSettings>()
                .AddSettings<ExperimentalSettings>()
                .AddSettings<GameLocatorSettings>()
            )
            .ConfigureLogging((_, builder) => builder
                .ClearProviders()
                .AddSimpleConsole()
                .SetMinimumLevel(LogLevel.Trace)
            )
            .Build();

        return host;
    }

    /// <summary>
    /// Constructs the host for the application, if <paramref name="slimMode"/> is true, the host will not register
    /// most of the services, and will only register what is required to proxy the app to the main process.
    /// <paramref name="isAvaloniaDesigner"/> should be set to true when constructing the host for the Avalonia Designer
    /// and will use the in memory database 
    /// </summary>
    private static IHost BuildHost(
        StartupMode startupMode,
        TelemetrySettings telemetrySettings,
        LoggingSettings loggingSettings,
        ExperimentalSettings experimentalSettings,
        GameLocatorSettings? gameLocatorSettings = null)
    {
        var observableTarget = new ObservableLoggingTarget();
        var host = new HostBuilder().ConfigureServices(services =>
        {
            var s = services.AddApp(
                telemetrySettings,
                startupMode: startupMode,
                experimentalSettings: experimentalSettings,
                gameLocatorSettings: gameLocatorSettings).Validate();

            if (loggingSettings.ShowExceptions)
                s.AddSingleton<IObservableExceptionSource, ObservableLoggingTarget>(_ => observableTarget);

            if (startupMode.IsAvaloniaDesigner)
            {
                s.OverrideSettingsForTests<DataModelSettings>(settings => settings with { UseInMemoryDataModel = true, });
            }
        })
        .ConfigureLogging((_, builder) => AddLogging(observableTarget, builder, loggingSettings, startupMode))
        .Build();

        return host;
    }

    private static void AddLogging(ObservableLoggingTarget observableLoggingTarget, ILoggingBuilder loggingBuilder, LoggingSettings settings, StartupMode startupMode)
    {
        var fs = FileSystem.Shared;
        var config = new NLog.Config.LoggingConfiguration();

        const string defaultLayout = "${processtime} [${level:uppercase=true}] (${logger}) ${message:withexception=true}";
        const string defaultHeader = "############ Nexus Mods App log file - ${longdate} ############";

        FileTarget fileTarget;
        if (startupMode.RunAsMain)
        {
            fileTarget = new FileTarget("file")
            {
                FileName = settings.MainProcessLogFilePath.ToPath(fs).GetFullPath(),
                ArchiveFileName = settings.MainProcessArchiveFilePath.ToPath(fs).GetFullPath(),
            };
        }
        else
        {
            fileTarget = new FileTarget("file")
            {
                FileName = settings.SlimProcessLogFilePath.ToPath(fs).GetFullPath(),
                ArchiveFileName = settings.SlimProcessArchiveFilePath.ToPath(fs).GetFullPath(),
            };
        }

        fileTarget.ArchiveOldFileOnStartup = true;
        fileTarget.MaxArchiveDays = settings.MaxArchivedFiles;
        fileTarget.Layout = defaultLayout;
        fileTarget.Header = defaultHeader;

        if (settings.LogToConsole)
        {
            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${processtime} [${level:uppercase=true}] ${message:withexception=true}",
            };
            config.AddRuleForAllLevels(consoleTarget);
        }

        config.AddRuleForAllLevels(fileTarget);
        config.AddRuleForAllLevels(observableLoggingTarget);


        // NOTE(erri120): RemoveLoggerFactoryFilter prevents
        // the global minimum level to take effect.
        // https://github.com/Nexus-Mods/NexusMods.App/issues/250
        var options = new NLogProviderOptions
        {
            RemoveLoggerFactoryFilter = false,
        };

        loggingBuilder.AddFilter("Microsoft", LogLevel.Warning);
        loggingBuilder.AddFilter("System", LogLevel.Warning);
        loggingBuilder.AddFilter("Avalonia.ReactiveUI", LogLevel.Warning);

        loggingBuilder.ClearProviders();
        loggingBuilder.SetMinimumLevel(settings.MinimumLevel);
        loggingBuilder.AddNLog(config, options);
    }

    /// <summary>
    /// Don't Delete this method. It's used by the Avalonia Designer.
    /// </summary>
    // ReSharper disable once UnusedMember.Local
    // ReSharper disable once UnusedMember.Global
    public static AppBuilder BuildAvaloniaApp()
    {
        var startupMode = new StartupMode()
        {
            RunAsMain = true,
            ShowUI = false,
            ExecuteCli = false,
            IsAvaloniaDesigner = true,
            Args = [],
            OriginalArgs = [],
        };

        var host = BuildHost(startupMode, 
            telemetrySettings: new TelemetrySettings(), 
            LoggingSettings.CreateDefault(OSInformation.Shared),
            experimentalSettings: new ExperimentalSettings()
        );
        
        host.StartAsync().GetAwaiter().GetResult();
        
        DesignerUtils.Activate(host.Services);
        
        return Startup.BuildAvaloniaApp(host.Services);
    }
}
