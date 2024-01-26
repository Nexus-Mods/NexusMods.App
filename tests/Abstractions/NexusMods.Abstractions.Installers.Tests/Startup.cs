using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.DependencyInjection;

namespace NexusMods.Abstractions.Installers.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSkippableFactSupport()
            .AddLogging(builder => builder.AddXUnit());
    }
}

