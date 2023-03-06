using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddAdvancedHttpDownloader()
                 .AddSingleton<SimpleHttpDownloader>()
                 .AddSingleton<AdvancedHttpDownloader>()
                 .AddSingleton<TemporaryFileManager>()
                 .AddSingleton<HttpClient>()
                 .AddSingleton<LocalHttpServer>()
                 .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

