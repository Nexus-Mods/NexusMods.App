using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk;

namespace NexusMods.Games.AdvancedInstaller.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDefaultServicesForTesting()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
