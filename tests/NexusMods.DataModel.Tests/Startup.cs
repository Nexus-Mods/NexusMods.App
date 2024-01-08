using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Common.GuidedInstaller;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
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
            .AddStandardGameLocators(false)
            .AddFileExtractors()
            .AddStubbedGameLocators()
            .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))

            // Diagnostics
            .AddSingleton<ILoadoutDiagnosticEmitter, DummyLoadoutDiagnosticEmitter>()
            .AddSingleton<IModDiagnosticEmitter, DummyModDiagnosticEmitter>()

            .AddLogging(builder => builder.AddXunitOutput())

            .Validate();
    }
}

