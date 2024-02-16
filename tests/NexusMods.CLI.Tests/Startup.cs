using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.SingleProcess;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStandardGameLocators(false)
                .AddStubbedGameLocators()
                .AddSingleton<CommandLineConfigurator>()
                .AddFileSystem()
                .AddDataModel(new DataModelSettings
                {
                    UseInMemoryDataModel = true
                })
                .AddFileExtractors()
                .AddCLI()
                .AddSingleton<HttpClient>()
                .AddHttpDownloader()
                .AddNexusWebApi(true)
                .AddActivityMonitor()
                .AddLoadoutAbstractions()
                .AddFileStoreAbstractions()
                .AddSerializationAbstractions()
                .AddGames()
                .AddInstallerTypes()
                .AddCrossPlatform()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                .AddSingleton<LocalHttpServer>()
                .AddLogging(builder => builder.AddXUnit())
                .Validate();
    }
}

