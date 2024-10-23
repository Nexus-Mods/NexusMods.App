using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;

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

