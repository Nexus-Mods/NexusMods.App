using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Settings;
using NexusMods.CrossPlatform;
using NexusMods.Paths;
using NexusMods.Settings;

namespace NexusMods.FileExtractor.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<TimeProvider>(_ => TimeProvider.System)
            .AddFileSystem()
            .AddSettingsManager()
            .AddFileExtractors()
            .AddCrossPlatform()
            .AddSettings<LoggingSettings>();
    }
}

