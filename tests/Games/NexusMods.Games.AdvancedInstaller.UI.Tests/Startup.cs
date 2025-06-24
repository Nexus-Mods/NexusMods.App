using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Games.Generic;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk;

namespace NexusMods.Games.AdvancedInstaller.UI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDefaultServicesForTesting()
            .AddGenericGameSupport()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
