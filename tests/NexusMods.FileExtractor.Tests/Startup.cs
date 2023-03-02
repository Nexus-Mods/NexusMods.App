using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileExtractors();
    }
}

