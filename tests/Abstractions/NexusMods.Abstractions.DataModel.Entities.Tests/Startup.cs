using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Activities;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.Abstractions.DataModel.Entities.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddDataModel()
            .AddActivityMonitor()
            .AddFileExtractors()
            .AddFileSystem()
            .AddGames()
            .AddStandardGameLocators(false)
            .AddLoadoutAbstractions()
            .AddFileStoreAbstractions()
            .AddStubbedGameLocators()
            .AddSerializationAbstractions()
            .AddInstallerTypes()
            .AddLogging(builder => builder.AddXUnit());
    }
}

