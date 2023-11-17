using Microsoft.Extensions.DependencyInjection;
using NexusMods.Common;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.MountAndBlade2Bannerlord.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<MountAndBlade2Bannerlord>(new Version("1.0.0.0"))
            .AddMountAndBladeBannerlord()
            .AddLogging(builder => builder.AddXunitOutput())
            .Validate();
    }
}

