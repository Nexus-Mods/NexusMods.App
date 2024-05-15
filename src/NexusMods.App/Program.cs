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
using NexusMods.App.Listeners;
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
            if (isMain)
            {

                LogMessages.StartingProcess(_logger, Environment.ProcessPath, Environment.ProcessId,
                    args
                );
                host.Services.GetRequiredService<NxmRpcListener>();
                Startup.Main(host.Services, []);
                return 0;
            }
            else
            {
                var client = host.Services.GetRequiredService<CliClient>();
                client.ExecuteCommand(args).Wait();
            }
        }
        finally
        {
            host.StopAsync().Wait(timeout: TimeSpan.FromSeconds(5));
        }

        return 0;
    }

    private static bool IsMainProcess(IReadOnlyList<string> args)
    {
        return args.Count == 0;
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

        if (settings.LogToConsole)
        {
            var consoleTarget = new ConsoleTarget("console")
            {
                Layout = "${processtime} [${level:uppercase=true}] ${message:withexception=true}",
            };
            config.AddRuleForAllLevels(consoleTarget);
        }

        config.AddRuleForAllLevels(fileTarget);


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
