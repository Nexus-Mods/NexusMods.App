using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.DataModel.RateLimiting;
using NexusMods.Networking.HttpDownloader;

namespace NexusMods.Networking.AdvancedHttpDownloader.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddAdvancedHttpDownloader();
        container.AddSingleton<TemporaryFileManager>();
        container.AddSingleton<HttpClient>();
        container.AddSingleton<LocalHttpServer>();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

