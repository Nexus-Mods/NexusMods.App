using Microsoft.Extensions.DependencyInjection;
using NexusMods.FileExtractor;
using NexusMods.Jobs;
using NexusMods.Paths;

namespace Examples;

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
            .AddSingleton<HttpClient>()
            .AddFileExtractors();
    }
}
