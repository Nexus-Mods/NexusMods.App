using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App;
using NexusMods.App.BuildInfo;
using NexusMods.Games.RedEngine;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using NexusMods.UI.Tests.Framework;

namespace NexusMods.UI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var path = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).Combine("temp").Combine(Guid.NewGuid().ToString());
        path.CreateDirectory();

        services.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
                .AddApp()
                .AddStubbedGameLocators()
                .AddSingleton<AvaloniaApp>()
                .AddLogging(builder => builder.AddXUnit())
                .Validate();
    }
}

