using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NexusMods.App;
using NexusMods.App.UI;
using NexusMods.CLI;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.DarkestDungeon;
using NexusMods.Games.Generic;
using NexusMods.Games.MountAndBladeBannerlord;
using NexusMods.Games.RedEngine;
using NexusMods.Games.Reshade;
using NexusMods.Games.TestHarness;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.StandardGameLocators;
using NLog.Extensions.Logging;
using NLog.Targets;
using static NexusMods.App.UI.Startup;

var host = Host.CreateDefaultBuilder(Environment.GetCommandLineArgs())
    .ConfigureLogging(AddLogging)
    .ConfigureServices((_, services) =>
    {
        services.AddCLI()
            .AddUI()
            .AddFileExtractors()
            .AddDataModel()
            .AddBethesdaGameStudios()
            .AddRedEngineGames()
            .AddGenericGameSupport()
            .AddMountAndBladeBannerlord()
            .AddReshade()
            .AddDarkestDungeon()
            .AddStandardGameLocators()
            .AddRenderers()
            .AddNexusWebApi()
            .AddAdvancedHttpDownloader()
            .AddTestHarness();

        services.AddSingleton<HttpClient>();
    }).Build();

if (args.Length > 0)
{
    var service = host.Services.GetRequiredService<CommandLineConfigurator>();
    var root = service.MakeRoot();

    var builder = new CommandLineBuilder(root)
        .UseDefaults()
        .Build();

    return await builder.InvokeAsync(args);
}

Main(host.Services, args);
return 0;

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