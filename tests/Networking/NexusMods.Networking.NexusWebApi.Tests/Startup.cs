using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Backend;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.FileHashes;
using NexusMods.Library;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Library;
using NexusMods.Sdk.Settings;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Networking.NexusWebApi.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddSerializationAbstractions()
            .AddFileSystem()
            .AddSettingsManager()
            .AddHttpDownloader()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<LocalHttpServer>()
            .AddNexusWebApi(true)
            .AddOSInterop()
            .AddRuntimeDependencies()
            .AddSettings<LoggingSettings>()
            .AddLoadoutAbstractions()
            .AddJobMonitor()
            .AddLibrary()
            .AddLibraryModels()
            .AddFileExtractors()
            .AddFileHashes()
            .AddDataModel() // this is required because we're also using NMA integration
            .OverrideSettingsForTests<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
            })
            .AddLogging(builder => builder.AddXunitOutput()
                .SetMinimumLevel(LogLevel.Debug))
            .Validate();
    }
}

