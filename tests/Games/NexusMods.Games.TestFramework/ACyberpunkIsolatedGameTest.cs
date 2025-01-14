using Microsoft.Extensions.DependencyInjection;
using NexusMods.CrossPlatform;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.Abstractions;

namespace NexusMods.Games.TestFramework;

/// <summary>
/// A override for the <see cref="AIsolatedGameTest{TGame}"/> for the <see cref="Cyberpunk2077Game"/>.
/// </summary>
public class ACyberpunkIsolatedGameTest<TTest>(ITestOutputHelper helper) : AIsolatedGameTest<TTest, Cyberpunk2077Game>(helper)
{
    protected override IServiceCollection AddServices(IServiceCollection services)
    {
        return base.AddServices(services)
            .AddCrossPlatform()
            .AddGenericGameSupport()
            .AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.61"))
            .AddRedEngineGames();
    }
}
