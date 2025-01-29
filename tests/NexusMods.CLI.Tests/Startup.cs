using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.Library.Models;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Settings;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.FileHashes;
using NexusMods.Jobs;
using NexusMods.Library;
using NexusMods.Networking.HttpDownloader;
using NexusMods.Networking.HttpDownloader.Tests;
using NexusMods.Networking.NexusWebApi;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.SingleProcess;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.CLI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.UI.Tests.Tests-{Guid.NewGuid()}";

        services.AddStandardGameLocators(false)
                .AddStubbedGameLocators()
                .AddSingleton<CommandLineConfigurator>()
                .AddFileSystem()
                .AddSettingsManager()
                .AddDataModel()
                .AddLibrary()
                .AddLibraryModels()
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
                .AddCrossPlatform()
                .AddSettings<LoggingSettings>()
                .AddLogging(builder => builder.AddXunitOutput().SetMinimumLevel(LogLevel.Trace))
                .AddSingleton<LocalHttpServer>()
                .AddLogging(builder => builder.AddXUnit())
                .Validate();
    }
}

