using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Reactive;
using System.Text.Json;
using Avalonia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.App.Listeners;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.Paths;
using NexusMods.SingleProcess;
using NLog.Extensions.Logging;
using NLog.Targets;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using ReactiveUI;

namespace NexusMods.App;

public class Program
{
    private static ILogger<Program> _logger = default!;

    // Run in debug mode if we are in debug mode and the debugger is attached. We use preprocessor flags here as
    // some AV software may be configured to flag processes that look for debuggers as malicious. So we don't even
    // look for a debugger unless we are in debug mode.
#if DEBUG
    private static bool _isDebug = Debugger.IsAttached;
#else
    private static bool _isDebug = false;
#endif


    [STAThread]
    public static async Task<int> Main(string[] args)
    {

        var isMain = IsMainProcess(args);

        var host = BuildHost(slimMode:!isMain);

        _logger = host.Services.GetRequiredService<ILogger<Program>>();
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            _logger.LogError(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };

        RxApp.DefaultExceptionHandler = Observer.Create<Exception>(ex =>
        {
            _logger.LogError(ex, "Unhandled exception");
        });



        // NOTE(erri120): DI is lazy by default and these services
        // do additional initialization inside their constructors.
        // We need to make sure their constructors are called to
        // finalize our OpenTelemetry configuration.
        //host.Services.GetService<TracerProvider>();
        //host.Services.GetService<MeterProvider>();


        _logger.LogDebug("Application starting in {Mode} mode", _isDebug ? "debug" : "release");
        var startup = host.Services.GetRequiredService<StartupDirector>();
        _logger.LogDebug("Calling startup handler");
        var result = await startup.Start(args, _isDebug);
        _logger.LogDebug("Startup handler returned {Result}", result);
        return result;
    }

    private static bool IsMainProcess(IReadOnlyList<string> args)
    {
        if (_isDebug) return true;
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
        var config = new AppConfig();
        var host = new HostBuilder()
            .ConfigureServices(services =>
            {
                // Bind the AppSettings class to the configuration and register it as a singleton service
                // Question to Reviewers: Should this be moved to AddApp?
                var appFolder = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory);
                var configJson = File.ReadAllText(appFolder.Combine("AppConfig.json").GetFullPath());

                // Note: suppressed because invalid config will throw.
                config = JsonSerializer.Deserialize<AppConfig>(configJson)!;
                config.Sanitize();
                services.AddApp(config, slimMode:slimMode).Validate();
            })
            .ConfigureLogging((_, builder) => AddLogging(builder, config.LoggingSettings))
            .Build();

        return host;
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
