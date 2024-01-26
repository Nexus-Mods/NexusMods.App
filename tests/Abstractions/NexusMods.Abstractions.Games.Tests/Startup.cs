using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexusMods.Abstractions.Games.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddLogging(builder => builder.AddXUnit());
    }
}

