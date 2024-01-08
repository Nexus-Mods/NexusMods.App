using Microsoft.Extensions.DependencyInjection;
using NexusMods.DataModel;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileSystem()
            .AddActivityMonitor()
            .AddFileExtractors();
    }
}

