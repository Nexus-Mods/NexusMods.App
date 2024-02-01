using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Abstractions.Diagnostics.Emitters;
using NexusMods.Abstractions.FileStore;
using NexusMods.Abstractions.Games;
using NexusMods.Abstractions.GuidedInstallers;
using NexusMods.Abstractions.Installers;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Serialization;
using NexusMods.Abstractions.Serialization.ExpressionGenerator;
using NexusMods.Activities;
using NexusMods.App.BuildInfo;
using NexusMods.DataModel.Tests.Diagnostics;
using NexusMods.FileExtractor;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.DataModel.Tests-{Guid.NewGuid()}");

        container
            .AddSingleton<IGuidedInstaller, NullGuidedInstaller>()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddFileSystem()
            .AddSingleton(new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddDataModel(new DataModelSettings(prefix))
            .AddGames()
            .AddStandardGameLocators(false)
            .AddFileExtractors()
            .AddStubbedGameLocators()
            .AddFileStoreAbstractions()
            .AddLoadoutAbstractions()
            .AddSerializationAbstractions()
            .AddActivityMonitor()
            .AddInstallerTypes()
            .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))

            // Diagnostics
            .AddSingleton<ILoadoutDiagnosticEmitter, DummyLoadoutDiagnosticEmitter>()
            .AddSingleton<IModDiagnosticEmitter, DummyModDiagnosticEmitter>()

            .AddLogging(builder => builder.AddXunitOutput())

            .Validate();
    }
}

