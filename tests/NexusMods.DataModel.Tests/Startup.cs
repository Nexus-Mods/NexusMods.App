using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Diagnostics.Emitters;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.RateLimiting;
using NexusMods.DataModel.Tests.Diagnostics;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = FileSystem.Shared
            .GetKnownPath(KnownPath.EntryDirectory)
            .Combine($"NexusMods.DataModel.Tests-{Guid.NewGuid()}");

        container
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddFileSystem()
            .AddSingleton(new TemporaryFileManager(FileSystem.Shared, prefix))
            .AddDataModel(new DataModelSettings(prefix))
            .AddStandardGameLocators(false)
            .AddFileExtractors()
            .AddStubbedGameLocators()
            .AddAllSingleton<IResource, IResource<ArchiveAnalyzer, Size>>(_ => new Resource<ArchiveAnalyzer, Size>("File Analysis"))
            .AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"))
            //.AddSingleton<IFileAnalyzer, ArchiveContentsCacheTests.MutatingFileAnalyzer>()
            .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))

            // Diagnostics
            .AddSingleton<ILoadoutDiagnosticEmitter, DummyLoadoutDiagnosticEmitter>()
            .AddSingleton<IModDiagnosticEmitter, DummyModDiagnosticEmitter>()

            .AddLogging(builder => builder.AddXUnit())

            .Validate();
    }
}

