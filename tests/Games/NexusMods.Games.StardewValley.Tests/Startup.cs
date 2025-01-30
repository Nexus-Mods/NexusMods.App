using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.App.BuildInfo;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

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
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<StardewValley>(new Version(1, 0), gameFiles)
            .AddStardewValley()
            .AddLogging(builder => builder.AddXUnit())
            .AddGames()
            .AddLoadoutAbstractions()
            .Validate();
    }
}
