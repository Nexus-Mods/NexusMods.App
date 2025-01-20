using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Paths;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.Steam.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddHttpDownloader()
            .AddSteam()
            .AddLoggingAuthenticationHandler()
            .AddLocalAuthFileStorage()
            .AddFileSystem()
            .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Trace));
    }
}

