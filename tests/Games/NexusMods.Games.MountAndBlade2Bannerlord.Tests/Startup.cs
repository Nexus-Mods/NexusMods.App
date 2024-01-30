using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
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
            .AddGames()
            .AddActivityMonitor()
            .AddDataModelBaseEntities()
            .AddInstallerTypes()
            .Validate();
    }
}

