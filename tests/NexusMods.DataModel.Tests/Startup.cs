using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.Paths.Extensions;
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
        var prefix = KnownFolders.EntryFolder.CombineUnchecked("tempTestData").CombineUnchecked(Guid.NewGuid().ToString());

        container.AddDataModel(prefix)
                 .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
                 .AddStandardGameLocators(false)
                 .AddSingleton<TemporaryFileManager>(s => new TemporaryFileManager(prefix.CombineUnchecked("tempFiles")))
                 .AddFileExtractors()
                 .AddSingleton(s => new FileCache(s.GetRequiredService<ILogger<FileCache>>(), KnownFolders.EntryFolder.CombineUnchecked("cache")))

                 .AddStubbedGameLocators()

                 .AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(s =>
            new Resource<FileContentsCache, Size>("File Analysis"))
                 .AddAllSingleton<IResource, IResource<IExtractor, Size>>(s =>
            new Resource<IExtractor, Size>("File Extraction"))

                 .AddSingleton<ITypeFinder>(s => new AssemblyTypeFinder(typeof(Startup).Assembly))
                 .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

