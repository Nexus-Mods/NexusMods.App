using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Backend;
using NexusMods.Games.TestFramework;
using NexusMods.Sdk;

namespace NexusMods.Games.Generic.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddGameServices()
            .Validate();
    }
}
