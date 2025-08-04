using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;

namespace NexusMods.Games.StardewValley.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var gameFiles = new Dictionary<RelativePath, byte[]>
        {
            { "Stardew Valley.deps.json", "{}"u8.ToArray() }
        };

        container
            .AddSkippableFactSupport()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<StardewValley>(new Version(1, 0), gameFiles)
            .AddStardewValley()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddLoadoutAbstractions()
            .Validate();
    }
}
