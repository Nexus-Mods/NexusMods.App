using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.App;
using NexusMods.CLI;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.DarkestDungeon;
using NexusMods.StandardGameLocators;
using NLog.Extensions.Logging;
using NLog.Targets;

var host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
    .ConfigureLogging(AddLogging)
    .ConfigureServices((_, services) =>
    {
        services.AddCLI()
            .AddBethesdaGameStudios()
            .AddDarkestDungeon()
            .AddStandardGameLocators()
            .AddRenderers()
            .AddCLIVerbs();
    }).Build();

var service = host.Services.GetRequiredService<CommandlineConfigurator>();
var root = service.MakeRoot();

var builder = new CommandLineBuilder(root)
    .UseDefaults()
    .Build();

return await builder.InvokeAsync(args);


void AddLogging(ILoggingBuilder loggingBuilder)
{
    var config = new NLog.Config.LoggingConfiguration();

    var fileTarget = new FileTarget("file")
    {
        FileName = "logs/nexusmods.app.current.log",
        ArchiveFileName = "logs/nexusmods.app.{##}.log",
        ArchiveOldFileOnStartup = true,
        MaxArchiveFiles = 10,
        Layout = "${processtime} [${level:uppercase=true}] (${logger}) ${message:withexception=true}",
        Header = "############ Nexus Mods App log file - ${longdate} ############"
    };

    var consoleTarget = new ConsoleTarget("console")
    {
        Layout = "${processtime} [${level:uppercase=true}] ${message:withexception=true}",
    };
        

    config.AddRuleForAllLevels(fileTarget);
    config.AddRuleForAllLevels(consoleTarget);

    loggingBuilder.ClearProviders();
    loggingBuilder.SetMinimumLevel(LogLevel.Information);
    loggingBuilder.AddNLog(config);
}