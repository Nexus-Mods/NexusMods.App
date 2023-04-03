using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.Paths.Utilities;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory)
            .CombineUnchecked("tempTestData")
            .CombineUnchecked(Guid.NewGuid().ToString());

        container.AddDataModel(new DataModelSettings(prefix))
                 .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Trace))
                 .AddStandardGameLocators(false)
                 .AddSingleton<TemporaryFileManager>(_ => new TemporaryFileManager(FileSystem.Shared, prefix.CombineUnchecked("tempFiles")))
                 .AddFileExtractors(new FileExtractorSettings())
                 .AddStubbedGameLocators()

                 .AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(_ =>
            new Resource<FileContentsCache, Size>("File Analysis"))
                 .AddAllSingleton<IResource, IResource<IExtractor, Size>>(_ =>
            new Resource<IExtractor, Size>("File Extraction"))

                 .AddSingleton<ITypeFinder>(_ => new AssemblyTypeFinder(typeof(Startup).Assembly))
                 .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}

