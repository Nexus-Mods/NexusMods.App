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
        var prefix = KnownFolders.EntryFolder.CombineUnchecked("tempTestData").CombineUnchecked(Guid.NewGuid().ToString());
        
        container.AddDataModel(prefix);
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        container.AddStandardGameLocators(false);
        container.AddSingleton<TemporaryFileManager>(s => new TemporaryFileManager(prefix.CombineUnchecked("tempFiles")));
        container.AddFileExtractors();
        container.AddSingleton(s => new FileCache(s.GetRequiredService<ILogger<FileCache>>(), KnownFolders.EntryFolder.CombineUnchecked("cache")));
        
        container.AddStubbedGameLocators();

        container.AddAllSingleton<IResource, IResource<FileContentsCache, Size>>(s =>
            new Resource<FileContentsCache, Size>("File Analysis"));
        container.AddAllSingleton<IResource, IResource<IExtractor, Size>>(s =>
            new Resource<IExtractor, Size>("File Extraction"));
        
        container.AddSingleton<ITypeFinder>(s => new AssemblyTypeFinder(typeof(Startup).Assembly));
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

