using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Sockets;
using System.Reactive;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Logging;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.Abstractions.Telemetry;
using NexusMods.App.Commandline;
using NexusMods.App.UI;
using NexusMods.App.UI.Settings;
using NexusMods.CrossPlatform;
using NexusMods.CrossPlatform.Process;
using NexusMods.DataModel;
using NexusMods.DataModel.SchemaVersions;
using NexusMods.Paths;
using NexusMods.ProxyConsole;
using NexusMods.Sdk;
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
        _logger = services.GetRequiredService<ILogger<Program>>();

        // NOTE(erri120): has to come before host startup
        CleanupUnresponsiveProcesses(services).Wait(timeout: TimeSpan.FromSeconds(10));

        // Okay to do wait here, as we are in the main process thread.
        host.StartAsync().Wait(timeout: TimeSpan.FromMinutes(5));

        if (startupMode.RunAsMain)
        {
            var dataModelSettings = services.GetRequiredService<ISettingsManager>().Get<DataModelSettings>();
            var fileSystem = services.GetRequiredService<IFileSystem>();
            var osInterop = services.GetRequiredService<IOSInterop>();

            var modelExists = dataModelSettings.MnemonicDBPath.ToPath(fileSystem).DirectoryExists();

            _ = Task.Run(async () =>
            {
                try
                {
                    var fileSystemMounts = await osInterop.GetFileSystemMounts();
                    var archiveLocation = dataModelSettings.ArchiveLocations[0].ToPath(fileSystem);
                    var mount = await osInterop.GetFileSystemMount(archiveLocation, fileSystemMounts);
                    if (mount is not null) _logger.LogInformation("Archives are stored at {Path} on mount {Mount}", archiveLocation, mount);
                    else _logger.LogWarning("Failed to find file system mount for {Path}", archiveLocation);
                }
                catch (Exception e)
                {
                    _logger.LogWarning(e, "Failed to get file system mounts");
                }
            });

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

        LogMessages.RuntimeInformation(_logger, RuntimeInformation.OSDescription, RuntimeInformation.FrameworkDescription, ApplicationConstants.InstallationMethod);
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
            // Wait for 15 seconds for the host to stop, otherwise kill the process
            if (!host.StopAsync().Wait(timeout: TimeSpan.FromSeconds(15)))
                Environment.Exit(0);
        }
    }

    private static async Task CleanupUnresponsiveProcesses(IServiceProvider serviceProvider)
    {
        // NOTE(erri120): this is a hack, see https://github.com/Nexus-Mods/NexusMods.App/issues/3633
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
        var syncFile = serviceProvider.GetRequiredService<SyncFile>();

        var (process, port) = syncFile.GetSyncInfo();
        if (process is null) return;

        var pid = process.Id;
        var canConnect = await CanConnectToProcess(logger, port, timeout: TimeSpan.FromSeconds(6), services: serviceProvider);
        if (canConnect) return;

        logger.LogWarning("Unable to connect to old process with PID `{PID}` on port `{Port}`, force closing process", pid, port);

        try
        {
            process.Kill(entireProcessTree: true);
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Exception killing old process `{PID}`", pid);
        }
    }

    private static async Task<bool> CanConnectToProcess(ILogger logger, int port, TimeSpan timeout, IServiceProvider services)
    {
        var client = services.GetRequiredService<CliClient>();
        var cts = new CancellationTokenSource(delay: timeout);

        try
        {
            // Run the heartbeat command to check if the process is responsive
            var commandTask = client.ExecuteCommand([StatusVerbs.HeartbeatCommand], AnsiConsole.Console);
            
            // Wait for the command to complete or the timeout to expire
            await commandTask.WaitAsync(cts.Token);
            return true;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Exception connecting to process on `{Port}`", port);
            return false;
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

            if (loggingSettings.ShowExceptions || telemetrySettings.IsEnabled)
                s.AddSingleton<IObservableExceptionSource, ObservableLoggingTarget>(_ => observableTarget);

            if (startupMode.IsAvaloniaDesigner)
            {
                s.OverrideSettingsForTests<DataModelSettings>(settings => settings with { UseInMemoryDataModel = true, });
            }
        })
        .ConfigureLogging((_, builder) => AddLogging(observableTarget, builder, loggingSettings, startupMode))
        .Build();

        return ApplicationConstants.IsDebug ? new DebuggingHost(host) : host;
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

file class DebuggingHost : IHost
{
    private readonly IHost _inner;
    private readonly ILogger _logger;

    public DebuggingHost(IHost inner)
    {
        _inner = inner;
        _logger = inner.Services.GetRequiredService<ILogger<DebuggingHost>>();
    }

    public Task StartAsync(CancellationToken cancellationToken) => _inner.StartAsync(cancellationToken);
    public Task StopAsync(CancellationToken cancellationToken) => _inner.StopAsync(cancellationToken);
    public IServiceProvider Services => _inner.Services;

    [SuppressMessage("ReSharper", "LocalizableElement")]
    public void Dispose()
    {
        // NOTE(erri120): I'm doing reflection and you can't stop me.
        if (_inner.Services is not ServiceProvider services) throw new NotSupportedException();

        var rootPropertyInfo = services.GetType().GetProperty(name: "Root", BindingFlags.Instance | BindingFlags.NonPublic);
        if (rootPropertyInfo is null) throw new NotSupportedException();

        var root = rootPropertyInfo.GetMethod?.Invoke(services, parameters: null);
        if (root is not IServiceScope scope) throw new NotSupportedException();
        if (scope.GetType().Name != "ServiceProviderEngineScope") throw new NotSupportedException();

        var fieldInfo = scope.GetType().GetField("_disposables", BindingFlags.NonPublic | BindingFlags.Instance);
        if (fieldInfo is null) throw new NotSupportedException();

        var fieldValue = fieldInfo.GetValue(scope);
        if (fieldValue is not List<object> tempList) throw new NotSupportedException();

        var disposableServices = tempList.ToArray();
        Log("Disposing `{0}` services", disposableServices.Length);

        Reloaded.Memory.Utilities.Box<bool> didDispose = false;

        _ = Task.Run(async () =>
        {
            var delay = TimeSpan.FromSeconds(5);
            await Task.Delay(delay);

            // ReSharper disable once AccessToModifiedClosure
            bool isDisposed = didDispose;
            if (isDisposed) return;

            Log("Failed to dispose `{0}` services withing `{1}` seconds", disposableServices.Length, delay.TotalSeconds);
            foreach (var disposableService in disposableServices)
            {
                var disposableType = disposableService switch
                {
                    IDisposable => "sync",
                    IAsyncDisposable => "async",
                    _ => throw new NotSupportedException(),
                };

                Log("Type={0} HashCode={1} DisposableType={2}", disposableService.GetType(), disposableService.GetHashCode(), disposableType);
            }

            // NOTE(erri120): If you landed here, that means the app is probably stuck shutting down.
            // Use this opportunity to further debug the issue. You can see all the services that need
            // disposing by inspecting the variables above and checking the logs.
            if (Debugger.IsAttached) Debugger.Break();
            // if (ApplicationConstants.IsCI) Environment.Exit(exitCode: 1);
        });

        _inner.Dispose();
        didDispose = true;
    }

    private void Log(string format, params object?[] arguments)
    {
        _logger.LogDebug(format, arguments);
        if (ApplicationConstants.IsCI) Console.WriteLine(format, arguments);
    }
}
