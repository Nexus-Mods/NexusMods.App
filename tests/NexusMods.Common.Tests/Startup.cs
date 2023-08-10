using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Common.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSkippableFactSupport()
            .AddLogging(builder => builder.AddXUnit());
    }
}

