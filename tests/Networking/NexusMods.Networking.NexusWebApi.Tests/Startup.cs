using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddHttpDownloader();
        container.AddSingleton<TemporaryFileManager>();
        container.AddSingleton<HttpClient>();
        container.AddSingleton<LocalHttpServer>();
        container.AddNexusWebApi(true);
        container.AddDataModel();
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

