using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.Games;
using NexusMods.DataModel.JsonConverters.ExpressionGenerator;
using NexusMods.DataModel.ModInstallers;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.DataModel.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        var prefix = Guid.NewGuid().ToString().ToRelativePath().RelativeTo(KnownFolders.EntryFolder.Join("tempTestData"));
        
        container.AddDataModel(prefix);
        container.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug));
        container.AddStandardGameLocators(false);
        container.AddSingleton<TemporaryFileManager>(s => new TemporaryFileManager(prefix.Join("tempFiles")));
        container.AddFileExtractors();
        container.AddSingleton(s => new FileCache(s.GetRequiredService<ILogger<FileCache>>(), KnownFolders.EntryFolder.Join("cache")));
        
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

