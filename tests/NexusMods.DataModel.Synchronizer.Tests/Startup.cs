using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.DataModel.Synchronizer.Tests;

public class Startup
{
    /// <summary>
    /// Why are Cyberpunk tests in a generic DataModel project, well it's, so we can test something that's close to real-world data. 
    /// </summary>
    /// <param name="container"></param>
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddSingleton<TimeProvider>(_ => TimeProvider.System)
            .AddSettings<LoggingSettings>()
            .AddSettingsManager()
            .AddFileSystem()
            .AddCrossPlatform()
            .AddGenericGameSupport();
    }
}

