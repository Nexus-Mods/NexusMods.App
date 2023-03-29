using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Games.FOMOD.Tests.Mocks;
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
            .AddSingleton<IExtractor, MockExtractor>()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IResource<IExtractor, Size>>(_ => new Resource<IExtractor, Size>("File Extraction"))
            .AddSingleton<FileExtractor.FileExtractor>()
            .AddSingleton<ICoreDelegates, MockDelegates>();
    }

    // ReSharper disable once UnusedMember.Global
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}
