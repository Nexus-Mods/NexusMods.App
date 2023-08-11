using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Games.TestFramework;

namespace NexusMods.Games.Generic.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddGenericGameSupport()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
