using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Settings;
using NexusMods.Paths;
using NexusMods.Settings;
using Xunit.DependencyInjection;

namespace NexusMods.CrossPlatform.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<TimeProvider>(_ => TimeProvider.System)
            .AddSettingsManager()
            .AddSettings<LoggingSettings>()
            .AddFileSystem()
            .AddCrossPlatform()
            .AddSkippableFactSupport()
            .AddLogging(builder => builder.AddXUnit());
    }
}

