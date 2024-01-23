using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.Common;
using NexusMods.CrossPlatform;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.DarkestDungeon.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<DarkestDungeon>(new Version())
            .AddDarkestDungeon()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddActivityMonitor()
            .AddDataModelEntities()
            .AddDataModelBaseEntities()
            .AddInstallerTypes()
            .AddCrossPlatform()
            .Validate();
    }
}
