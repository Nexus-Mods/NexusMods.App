using Microsoft.Extensions.DependencyInjection;
using NexusMods.CLI;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.TestFramework;
using NexusMods.SingleProcess;
using NexusMods.StandardGameLocators.TestHelpers;
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
            .AddSingleton<CommandLineConfigurator>()
            .AddBethesdaGameStudios()
            .AddGenericGameSupport()
            .AddFomod()
            .AddCLI()
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddLogging(builder => builder.AddXunitOutput())
            .Validate();
    }
}
