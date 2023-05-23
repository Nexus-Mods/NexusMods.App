using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddStandardGameLocators(false)
                .AddStubbedGameLocators()
                .AddFileSystem()
                .AddDataModel()
                .AddFileExtractors()
                .AddCLI()
                .AddSingleton<HttpClient>()
                .AddHttpDownloader()
                .AddNexusWebApi(true)
                .AddAllScoped<IRenderer, LoggingRenderer>()
                .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
                .Validate();
    }
}

