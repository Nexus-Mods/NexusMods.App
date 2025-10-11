using Microsoft.Extensions.DependencyInjection;
using NexusMods.Backend;
using NexusMods.Paths;

namespace NexusMods.Jobs.Tests;

public static class DIHelpers
{
    public static void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.Examples.Tests-{Guid.NewGuid()}";
        var prefix = FileSystem.Shared.GetKnownPath(baseKnownPath).Combine(baseDirectory);
        
        services
            .AddFileSystem()
            .AddSingleton<TemporaryFileManager>(_ => new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddJobMonitor()
            .AddSingleton<HttpClient>();
    }
}
