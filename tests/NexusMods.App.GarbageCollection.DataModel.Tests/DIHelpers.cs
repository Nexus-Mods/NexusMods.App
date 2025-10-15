using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Backend;
using NexusMods.Games.TestFramework;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
namespace NexusMods.App.GarbageCollection.DataModel.Tests;

public static class DIHelpers
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddDefaultServicesForTesting()
            .AddStandardGameLocators(false)
            .AddGameServices()
            .AddLoadoutAbstractions()
            .AddStubbedGameLocators()
            .AddGames();
    }
}
