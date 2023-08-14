using System.CommandLine.Builder;
using System.CommandLine.Parsing;
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
using NLog.Extensions.Logging;
using NLog.Targets;
using ReactiveUI;

namespace NexusMods.App;

public class Program
{
    private static ILogger<Program> _logger = default!;

    [STAThread]
    public static async Task<int> Main(string[] args)
    {
        var host = BuildHost();

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

        if (args.Length > 0)
        {
            var service = host.Services.GetRequiredService<CommandLineConfigurator>();
            var root = service.MakeRoot();

            var builder = new CommandLineBuilder(root)
                .UseDefaults()
                .Build();

            return await builder.InvokeAsync(args);
        }
        else
        {
            var selector = host.Services.GetRequiredService<CliOptionSelector>();
            var renderers = host.Services.GetServices<IRenderer>();
            selector.Renderer = renderers.FirstOrDefault(r => r.Name == "console") ?? renderers.First();
        }

        // Start listeners only available in GUI mode
        host.Services.GetRequiredService<NxmRpcListener>();
        Startup.Main(host.Services, args);
        return 0;
    }

    public static IHost BuildHost()
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
                services.AddSingleton(config);
                services.AddApp(config).Validate();
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
    public static AppBuilder BuildAvaloniaApp()
    {
        var host = BuildHost();
        return Startup.BuildAvaloniaApp(host.Services);
    }
}
