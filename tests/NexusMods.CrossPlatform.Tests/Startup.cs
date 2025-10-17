using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Sdk.Settings;
using NexusMods.Backend;
using NexusMods.Paths;
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
            .AddOSInterop()
            .AddRuntimeDependencies()
            .AddSkippableFactSupport()
            .AddLogging(builder => builder.AddXUnit());
    }
}

