using Microsoft.Extensions.DependencyInjection;

namespace NexusMods.Jobs.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services) => DIHelpers.ConfigureServices(services);
}

