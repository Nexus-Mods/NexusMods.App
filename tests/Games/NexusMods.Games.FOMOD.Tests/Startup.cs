using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.Games.BethesdaGameStudios;
using NexusMods.Games.BethesdaGameStudios.SkyrimSpecialEdition;
using NexusMods.Games.Generic;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Games.FOMOD.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDefaultServicesForTesting()
            .AddBethesdaGameStudios()
            .AddUniversalGameLocator<SkyrimSpecialEdition>(new Version("1.6.659.0"))
            .AddGenericGameSupport()
            .AddFileStoreAbstractions()
            .AddFomod()
            .AddSingleton<ICoreDelegates, MockDelegates>()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddActivityMonitor()
            .AddSerializationAbstractions()
            .AddInstallerTypes()
            .Validate();
    }
}
