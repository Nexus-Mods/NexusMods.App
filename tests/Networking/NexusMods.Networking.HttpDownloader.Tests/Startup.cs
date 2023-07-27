using System.Globalization;
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
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.Networking.HttpDownloader.Tests-{Guid.NewGuid()}");

        container.AddAdvancedHttpDownloader()
                 .AddSingleton<SimpleHttpDownloader>()
                 .AddSingleton<AdvancedHttpDownloader>()
                 .AddFileSystem()
                 .AddSingleton(new TemporaryFileManager(FileSystem.Shared, prefix))
                 .AddSingleton<HttpClient>()
                 .AddSingleton<LocalHttpServer>()
                 .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

