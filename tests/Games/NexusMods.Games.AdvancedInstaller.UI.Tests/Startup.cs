using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App.BuildInfo;
using NexusMods.Games.Generic;
using NexusMods.Games.TestFramework;

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
