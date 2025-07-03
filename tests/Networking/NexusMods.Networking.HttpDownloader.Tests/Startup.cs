using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Sdk;

namespace NexusMods.Networking.HttpDownloader.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.Networking.HttpDownloader.Tests-{Guid.NewGuid()}");

        container
            .AddDefaultServicesForTesting(prefix)
            .AddSingleton<LocalHttpServer>()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}

