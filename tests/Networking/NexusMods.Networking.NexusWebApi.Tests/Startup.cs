using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform.Process;
using NexusMods.DataModel;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Paths;
using NexusMods.Settings;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddFileSystem()
            .AddSettingsManager()
            .AddSingleton<HttpClient>()
            .AddHttpDownloader()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IProcessFactory, ProcessFactory>()
            .AddSingleton<LocalHttpServer>()
            .AddNexusWebApi(true)
            .AddActivityMonitor()
            .AddLoadoutAbstractions()
            .AddDataModel() // this is required because we're also using NMA integration
            .AddLogging(builder => builder.AddXunitOutput()
                .SetMinimumLevel(LogLevel.Debug))
            .Validate();
    }
}

