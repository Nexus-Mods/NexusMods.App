using Microsoft.Extensions.DependencyInjection;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.Games.FOMOD;
using NexusMods.Games.Generic;
using NexusMods.Games.RedEngine;
using NexusMods.Games.RedEngine.Cyberpunk2077;
using NexusMods.Games.TestFramework;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Networking.Downloaders.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.Downloaders.Tests-{Guid.NewGuid()}";
        
        services
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddDefaultServicesForTesting()
            .AddUniversalGameLocator<Cyberpunk2077Game>(new Version("1.61"))
            .AddStubbedGameLocators()
            .AddGenericGameSupport()
            .AddRedEngineGames()
            .AddFomod()
            .AddDownloaders()
            .OverrideSettingsForTests<DownloadSettings>(settings => settings with
            {
                OngoingDownloadLocation = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Downloads/Ongoing"),
            })
            .AddSingleton<LocalHttpServer>()
            .Validate();
    }
}

