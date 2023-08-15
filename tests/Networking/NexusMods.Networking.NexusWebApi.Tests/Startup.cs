using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Networking.NexusWebApi.NMA;
using NexusMods.Paths;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddFileSystem()
            .AddSingleton<HttpClient>()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddHttpDownloader()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IProcessFactory, ProcessFactory>()
            .AddSingleton<LocalHttpServer>()
            .AddNexusWebApi()
            .AddNexusWebApiNmaIntegration(true)
            .AddDataModel(new DataModelSettings()
            {
                UseInMemoryDataModel = true
            })
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}

