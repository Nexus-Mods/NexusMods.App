using Microsoft.Extensions.DependencyInjection;
using NexusMods.Activities;
using NexusMods.Paths;
using NexusMods.Settings;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddFileSystem()
            .AddSingleton(OSInformation.Shared)
            .AddSettingsManager()
            .AddActivityMonitor()
            .AddFileExtractors();
    }
}

