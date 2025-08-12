using Microsoft.Extensions.DependencyInjection;
using NexusMods.Games.TestFramework;

namespace NexusMods.Networking.ModUpdates.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddDefaultServicesForTesting();
    }
}

