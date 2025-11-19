using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Backend;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.Games.FileHashes;
using NexusMods.Library;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Sdk;
using NexusMods.Sdk.Settings;
using NexusMods.SingleProcess;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";

        services
            .AddDatabaseModels()
                .AddSingleton<CommandLineConfigurator>()
                .AddFileSystem()
                .AddSettingsManager()
                .AddDataModel()
                .AddLibrary()
                .AddJobMonitor()
                .OverrideSettingsForTests<DataModelSettings>(settings => settings with
                {
                    UseInMemoryDataModel = true,
                    MnemonicDBPath = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/MnemonicDB.rocksdb"),
                    ArchiveLocations = [
                        new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Archives"),
                    ],
                })
                .AddFileExtractors()
                .AddFileHashes()
                .AddCLI()
                .AddHttpDownloader()
                .AddNexusWebApi(true)
                .AddLoadoutAbstractions()
                .AddSerializationAbstractions()
                .AddGames()
                .AddGameServices()
                .AddOSInterop()
                .AddRuntimeDependencies()
                .AddSettings<LoggingSettings>()
                .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Trace))
                .AddSingleton<LocalHttpServer>()
                .AddLogging(builder => builder.AddXUnit())
                .Validate();
    }
}

