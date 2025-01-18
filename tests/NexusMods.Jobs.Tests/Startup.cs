using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace NexusMods.Jobs.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddJobMonitor()
            .AddLogging(builder => builder.AddXUnit());
    }
}

