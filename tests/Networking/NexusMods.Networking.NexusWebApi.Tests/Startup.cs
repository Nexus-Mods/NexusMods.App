using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Settings;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
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
            .AddSingleton<LocalHttpServer>()
            .AddNexusWebApi(true)
            .AddActivityMonitor()
            .AddCrossPlatform()
            .AddSettings<LoggingSettings>()
            .AddLoadoutAbstractions()
            .AddDataModel() // this is required because we're also using NMA integration
            .AddLogging(builder => builder.AddXunitOutput()
                .SetMinimumLevel(LogLevel.Debug))
            .Validate();
    }
}

