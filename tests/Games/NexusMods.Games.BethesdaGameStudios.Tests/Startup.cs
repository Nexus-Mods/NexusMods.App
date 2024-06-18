using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CLI;
using NexusMods.CrossPlatform;
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
            .AddUniversalGameLocator<SkyrimSpecialEdition.SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddUniversalGameLocator<SkyrimLegendaryEdition.SkyrimLegendaryEdition>(new Version("1.9.32.0"))
            .AddSingleton<CommandLineConfigurator>()
            .AddBethesdaGameStudios()
            .AddGames()
            .AddSerializationAbstractions()
            .AddInstallerTypes()
            .AddGenericGameSupport()
            .AddLoadoutAbstractions()
            .AddFileStoreAbstractions()
            .AddFomod()
            .AddCLI()
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddLogging(builder => builder.AddXunitOutput())
            .Validate();
    }
}
