using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.TestFramework;
using Xunit.DependencyInjection;

namespace NexusMods.Networking.ModUpdates.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSkippableFactSupport()
            .AddDefaultServicesForTesting();
    }
}

