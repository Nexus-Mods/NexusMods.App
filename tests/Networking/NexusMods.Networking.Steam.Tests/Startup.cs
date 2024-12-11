using Microsoft.Extensions.DependencyInjection;
using NexusMods.Paths;

namespace NexusMods.Networking.Steam.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSingleton<HttpClient>()
            .AddSteamStore()
            .AddLoggingAuthenticationHandler()
            .AddLocalAuthFileStorage()
            .AddFileSystem();
    }
}

