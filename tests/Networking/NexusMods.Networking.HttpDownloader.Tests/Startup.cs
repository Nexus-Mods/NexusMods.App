using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Jobs;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.DataModel;
using NexusMods.Games.TestFramework;
using NexusMods.Jobs;
using NexusMods.Paths;
using NexusMods.Settings;

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
            .AddSingleton<SimpleHttpDownloader>()
            .AddSingleton<LocalHttpServer>()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}

