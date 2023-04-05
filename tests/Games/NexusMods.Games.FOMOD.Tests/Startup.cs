using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.Games.FOMOD.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddDataModel(new DataModelSettings(FileSystem.Shared.GetKnownPath(KnownPath.EntryDirectory).CombineUnchecked("FomodTests")))
            .AddFileExtractors()
            .AddFomod()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"))
            .AddSingleton<IResource<FileContentsCache, Size>>(_ => new Resource<FileContentsCache, Size>("Dummy Contents Cache"))
            .AddSingleton<IResource<FileHashCache, Size>>(_ => new Resource<FileHashCache, Size>("Dummy Hash Cache"))
            .AddSingleton<FileExtractor.FileExtractor>()
            .AddSingleton<ICoreDelegates, MockDelegates>();
    }

    // ReSharper disable once UnusedMember.Global
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}
