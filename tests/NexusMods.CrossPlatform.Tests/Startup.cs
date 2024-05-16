using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Paths;
using Xunit.DependencyInjection;

namespace NexusMods.CrossPlatform.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddFileSystem()
            .AddCrossPlatform()
            .AddSkippableFactSupport()
            .AddLogging(builder => builder.AddXUnit());
    }
}

