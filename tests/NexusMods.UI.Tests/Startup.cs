using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.DataModel.Entities;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.App;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
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
        var config = new AppConfig
        {
            DataModelSettings =
            {
                UseInMemoryDataModel = true
            }
        };

        services.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"))
                .AddApp(config: config)
                .AddStubbedGameLocators()
                .AddLoadoutAbstractions()
                .AddFileStoreAbstractions()
                .AddSingleton<AvaloniaApp>()
                .AddLogging(builder => builder.AddXUnit())
                .Validate();
    }
}

