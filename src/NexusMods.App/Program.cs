using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.DataModel;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.SingleProcess;
using NLog.Extensions.Logging;
using NLog.Targets;
using ReactiveUI;

namespace NexusMods.App;

public class Program
{
    private static ILogger<Program> _logger = default!;

    [STAThread]
    public static int Main(string[] args)
    {
        MainThreadData.SetMainThread();

        TelemetrySettings telemetrySettings;
        LoggingSettings loggingSettings;
        using (var settingsHost = BuildSettingsHost())
        {
            var settingsManager = settingsHost.Services.GetRequiredService<ISettingsManager>();
            telemetrySettings = settingsManager.Get<TelemetrySettings>();
            loggingSettings = settingsManager.Get<LoggingSettings>();
        }

        var isMain = IsMainProcess(args);
        var host = BuildHost(
            slimMode: !isMain,
            telemetrySettings,
            loggingSettings
        );

        // Okay to do wait here, as we are in the main process thread.
        host.StartAsync().Wait(timeout: TimeSpan.FromMinutes(5));

        _logger = host.Services.GetRequiredService<ILogger<Program>>();
        LogMessages.RuntimeInformation(_logger, RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription);
        LogMessages.StartingProcess(_logger, Environment.ProcessPath, Environment.ProcessId, args.Length, args);

        TaskScheduler.UnobservedTaskException += (sender, eventArgs) =>
        {
            LogMessages.UnobservedTaskException(_logger, eventArgs.Exception, sender, sender?.GetType());
            eventArgs.SetObserved();
        };

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            LogMessages.UnobservedReactiveThrownException(_logger, ex);
        });

        if (MainThreadData.IsDebugMode)
        {
            _logger.LogInformation("Starting the application in single-process mode with an attached debugger");
        }
        else
        {
            _logger.LogInformation("Starting the application in release mode without an attached debugger");
        }

        var startup = host.Services.GetRequiredService<StartupDirector>();

        var managerTask = Task.Run(async () =>
        {
            try
            {
                _logger.LogTrace("Calling startup handler");
                return await startup.Start(args, MainThreadData.IsDebugMode);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Exception in startup handler");
                Environment.Exit(-1);
                throw;
            }
            finally
            {
                try
                {
                    if (!MainThreadData.IsDebugMode)
                    {
                        _logger.LogTrace("Shutting down main thread in release mode");
                        MainThreadData.Shutdown();
                    }
                    else
                    {
                        _logger.LogInformation("The main thread won't be shutdown in debug mode");
                    }
                }
                catch (Exception e)
                {
                    _logger.LogCritical(e, "Error shutting down main thread");
                }
            }
        });

        // The UI *must* be started on the main thread, according to the Avalonia docs, although it
        // seems to work fine on some platforms (this behavior is not guaranteed). So when we need to open a new
        // window, the handler will enqueue an action to be run on the main thread.
        while (!MainThreadData.GlobalShutdownToken.IsCancellationRequested)
        {
            if (MainThreadData.MainThreadActions.TryDequeue(out var action))
            {
                try
                {
                    action();
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Exception running main thread action");
                }
                continue;
            }
            Thread.Sleep(250);
        }

        _logger.LogInformation("Startup handler returned {Result}", managerTask.Result);
        return managerTask.Result;
    }

    private static bool IsMainProcess(IReadOnlyList<string> args)
    {
        if (MainThreadData.IsDebugMode) return true;
        return args.Count == 1 && args[0] == StartupHandler.MainProcessVerb;
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
        bool slimMode,
        TelemetrySettings telemetrySettings,
        LoggingSettings loggingSettings,
        bool isAvaloniaDesigner = false)
    {
        var host = new HostBuilder()
            .ConfigureServices(services =>
                {
                    var s = services.AddApp(telemetrySettings, slimMode: slimMode).Validate();

                    if (isAvaloniaDesigner)
                    {
                        s.OverrideSettingsForTests<DataModelSettings>(settings => settings with
                            {
                                UseInMemoryDataModel = true,
                            }
                        );
                    }
                }
            )
            .ConfigureLogging((_, builder) => AddLogging(builder, loggingSettings, isMainProcess: !slimMode))
            .Build();

        return host;
    }

    private static void AddLogging(ILoggingBuilder loggingBuilder, LoggingSettings settings, bool isMainProcess)
    {
        var fs = FileSystem.Shared;
        var config = new NLog.Config.LoggingConfiguration();

        const string defaultLayout = "${processtime} [${level:uppercase=true}] (${logger}) ${message:withexception=true}";
        const string defaultHeader = "############ Nexus Mods App log file - ${longdate} ############";

        FileTarget fileTarget;
        if (isMainProcess)
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

        var consoleTarget = new ConsoleTarget("console")
        {
            Layout = "${processtime} [${level:uppercase=true}] ${message:withexception=true}",
        };

        config.AddRuleForAllLevels(fileTarget);
        config.AddRuleForAllLevels(consoleTarget);

        // NOTE(erri120): RemoveLoggerFactoryFilter prevents
        // the global minimum level to take effect.
        // https://github.com/Nexus-Mods/NexusMods.App/issues/250
        var options = new NLogProviderOptions
        {
            RemoveLoggerFactoryFilter = false,
        };

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
        var host = BuildHost(slimMode: false, 
            telemetrySettings: new TelemetrySettings(), 
            LoggingSettings.CreateDefault(OSInformation.Shared),
            isAvaloniaDesigner: true);
        
        DesignerUtils.Activate(host.Services);
        
        return Startup.BuildAvaloniaApp(host.Services);
    }
}
