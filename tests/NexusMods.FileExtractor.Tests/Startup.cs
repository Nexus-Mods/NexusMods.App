using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileSystem()
            .AddFileExtractors();
    }
}

