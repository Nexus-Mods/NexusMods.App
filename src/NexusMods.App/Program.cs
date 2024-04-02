using System.Reactive;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        // I'm not 100% sure how to wire this up to cleanly pass settings
        // to ConfigureLogging; since the DI container isn't built until the host is.
        var config = ReadAppConfig(new AppConfig());
        var host = new HostBuilder()
            .ConfigureServices(services => services.AddApp(config, slimMode:slimMode).Validate())
            .ConfigureLogging((_, builder) => AddLogging(builder, config.LoggingSettings))
            .Build();

        return host;
    }

    private static AppConfig ReadAppConfig(AppConfig existingConfig)
    {
        // Read an App Config from the entry directory and sanitize if it exists.
        var configJson = TryReadConfig();

        if (configJson != null)
        {
            // If we can't deserialize, use default.
            try
            {
                existingConfig = JsonSerializer.Deserialize<AppConfig>(configJson)!;
            }
            catch (Exception)
            {
                /* Ignored */
            }

            existingConfig.Sanitize(FileSystem.Shared);
        }
        else
        {
            // No custom config so use default.
            existingConfig.Sanitize(FileSystem.Shared);
        }

        return existingConfig;
    }

    private static string? TryReadConfig()
    {
        // Try to read an `AppConfig.json` from the entry directory
        const string configFileName = "AppConfig.json";

        // TODO: NexusMods.Paths needs ReadAllText API. For now we delegate to standard library because source is `FileSystem.Shared`.

        var fs = FileSystem.Shared;
        if (fs.OS.IsLinux)
        {
            // On AppImage (Linux), 'OWD' should take precedence over the entry directory if it exists.
            // https://docs.appimage.org/packaging-guide/environment-variables.html
            var owd = Environment.GetEnvironmentVariable("OWD");
            if (!string.IsNullOrEmpty(owd))
            {
                try
                {
                    return File.ReadAllText(fs.FromUnsanitizedFullPath(owd).Combine(configFileName)
                        .GetFullPath());
                }
                catch (Exception)
                {
                    /* Ignored */
                }
            }
        }

        // Try App Folder
        var appFolder = fs.GetKnownPath(KnownPath.EntryDirectory);
        try
        {
            return File.ReadAllText(appFolder.Combine(configFileName).GetFullPath());
        }
        catch (Exception)
        {
            /* Ignored */
        }

        // Config doesn't exist.
        return null;
    }

    static void AddLogging(ILoggingBuilder loggingBuilder, ILoggingSettings settings)
    {
        var config = new NLog.Config.LoggingConfiguration();

        var fileTarget = new FileTarget("file")
        {
            FileName = settings.FilePath.GetFullPath(),
            ArchiveFileName = settings.ArchiveFilePath.GetFullPath(),
            ArchiveOldFileOnStartup = true,
            MaxArchiveFiles = settings.MaxArchivedFiles,
            Layout = "${processtime} [${level:uppercase=true}] (${logger}) ${message:withexception=true}",
            Header = "############ Nexus Mods App log file - ${longdate} ############"
        };

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
            RemoveLoggerFactoryFilter = false
        };

        loggingBuilder.ClearProviders();
#if DEBUG
        loggingBuilder.SetMinimumLevel(LogLevel.Debug);
#elif TRACE
        loggingBuilder.SetMinimumLevel(LogLevel.Trace);
#else
        loggingBuilder.SetMinimumLevel(LogLevel.Information);
#endif

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
