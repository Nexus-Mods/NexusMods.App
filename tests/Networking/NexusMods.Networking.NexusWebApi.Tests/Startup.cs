using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NexusMods.Common;
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
        var mockOSInterop = new Mock<IOSInterop>();

        container
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddHttpDownloader()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IProcessFactory, ProcessFactory>()
            .AddSingleton(mockOSInterop.Object)
            .AddSingleton<LocalHttpServer>()
            .AddNexusWebApi(true)
            .AddDataModel();
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

