using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Games.BethesdaGameStudios;
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
            .AddFomod()
            .AddSingleton<ICoreDelegates, MockDelegates>()
            .AddLogging(builder => builder.AddXUnit())
            .Validate();
    }
}
