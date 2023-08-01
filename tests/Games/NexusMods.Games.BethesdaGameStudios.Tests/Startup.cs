using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.BethesdaGameStudios.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddUniversalGameLocator<SkyrimLegendaryEdition>(new Version("1.9.32.0"))
            .AddBethesdaGameStudios()
            .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

