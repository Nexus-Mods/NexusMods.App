using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.CLI;
using NexusMods.Common;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;
using IResource = NexusMods.DataModel.RateLimiting.IResource;

namespace NexusMods.Updater.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<HttpClient>()
            .AddAllSingleton<IProcessFactory, FakeProcessFactory>(x => new FakeProcessFactory(0))
            .AddSingleton<IRenderer>(s => new App.CLI.Renderers.Spectre(Array.Empty<IResource>()))
            .AddFileSystem()
            .AddUpdater()
            .AddSingleton(new TemporaryFileManager(FileSystem.Shared))
            .AddAdvancedHttpDownloader();

    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

