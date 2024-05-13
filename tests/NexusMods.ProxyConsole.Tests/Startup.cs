using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.ProxyConsole;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Spectre.ProxyConsole.Tests;

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
