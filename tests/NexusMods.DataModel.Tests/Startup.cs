using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Abstractions.Settings;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.CrossPlatform;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.Settings;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        const KnownPath baseKnownPath = KnownPath.EntryDirectory;
        var baseDirectory = $"NexusMods.DataModel.Tests-{Guid.NewGuid()}";

        var prefix = FileSystem.Shared
            .GetKnownPath(baseKnownPath)
            .Combine(baseDirectory);

        container
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddFileSystem()
            .AddSingleton(new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddSettingsManager()
            .AddDataModel()
            .OverrideSettings<DataModelSettings>(settings => settings with
            {
                UseInMemoryDataModel = true,
                DataStoreFilePath = new ConfigurablePath(baseKnownPath, $"{baseDirectory}/DataStore.sqlite"),
                ArchiveLocations = [
                    new ConfigurablePath(baseKnownPath, $"{baseDirectory}/Archives"),
                ],
            })
            .AddGames()
            .AddStandardGameLocators(false)
            .AddFileExtractors()
            .AddStubbedGameLocators()
            .AddFileStoreAbstractions()
            .AddLoadoutAbstractions()
            .AddSerializationAbstractions()
            .AddActivityMonitor()
            .AddInstallerTypes()
            .AddCrossPlatform()
            .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))
            .AddLogging(builder => builder.AddXunitOutput())
            .Validate();
    }
}

