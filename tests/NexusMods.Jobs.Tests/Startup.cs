using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;

namespace NexusMods.Jobs.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.Examples.Tests-{Guid.NewGuid()}";
        var prefix = FileSystem.Shared.GetKnownPath(baseKnownPath).Combine(baseDirectory);

        container
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>(_ => new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddJobMonitor()
            .AddSingleton<HttpClient>();
    }
}

