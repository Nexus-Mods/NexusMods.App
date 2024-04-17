using System.Diagnostics;
using System.Reactive;
using System.Runtime.InteropServices;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.App.UI;
using NexusMods.Paths;
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

        var isMain = IsMainProcess(args);
        var host = BuildHost(slimMode: !isMain);

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

    /// <summary>
    /// Constructs the host for the application, if <paramref name="slimMode"/> is true, the host will not register
    /// most of the services, and will only register what is required to proxy the app to the main process.
    /// </summary>
    /// <param name="slimMode"></param>
    /// <returns></returns>
    public static IHost BuildHost(bool slimMode = false)
    {
        var host = new HostBuilder()
            .ConfigureServices(services => services.AddApp(slimMode: slimMode).Validate())
            .ConfigureLogging((_, builder) => AddLogging(builder, isMainProcess: !slimMode))
            .Build();

        return host;
    }

    private static void AddLogging(ILoggingBuilder loggingBuilder, bool isMainProcess)
    {
        var fs = FileSystem.Shared;
        var settings = LoggingSettings.CreateDefault(fs.OS);

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
        var host = BuildHost();
        DesignerUtils.Activate(host.Services);
        return Startup.BuildAvaloniaApp(host.Services);
    }
}
