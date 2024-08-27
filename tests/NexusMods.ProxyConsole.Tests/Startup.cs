using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.ProxyConsole.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultRenderers()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddLogging(builder => builder.AddXunitOutput());

    }
}
