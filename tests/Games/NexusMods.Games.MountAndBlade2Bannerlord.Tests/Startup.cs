using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
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
            .AddSerializationAbstractions()
            .AddFileStoreAbstractions()
            .AddLoadoutAbstractions()
            .AddInstallerTypes()
            .Validate();
    }
}

