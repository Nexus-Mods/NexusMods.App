using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.BladeAndSorcery.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<BladeAndSorcery>(new Version())
            .AddBladeAndSorcery()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddActivityMonitor()
            .AddDataModelBaseEntities()
            .AddInstallerTypes()
            .Validate();
    }
}
