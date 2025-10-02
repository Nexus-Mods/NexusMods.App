using Microsoft.Extensions.DependencyInjection;
using NexusMods.Backend;
using NexusMods.CrossPlatform;
using NexusMods.Paths;
using NexusMods.Sdk.Settings;

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

