using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.Networking.HttpDownloader.Tests-{Guid.NewGuid()}");

        container.AddAdvancedHttpDownloader()
                 .AddActivityMonitor()
                 .AddSingleton<SimpleHttpDownloader>()
                 .AddSingleton<AdvancedHttpDownloader>()
                 .AddFileSystem()
                 .AddSingleton(new TemporaryFileManager(FileSystem.Shared, prefix))
                 .AddSingleton<HttpClient>()
                 .AddSingleton<LocalHttpServer>()
                 .AddLogging(builder => builder.AddXUnit())
                 .Validate();
    }
}

