using FomodInstaller.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.DataModel.RateLimiting;
using NexusMods.FileExtractor;
using NexusMods.FileExtractor.Extractors;
using NexusMods.Paths;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.FOMOD.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .AddSingleton<IExtractor, MockExtractor>()
            .AddSingleton<TemporaryFileManager>()
            .AddSingleton<IResource<IExtractor, Size>>(s => new Resource<IExtractor, Size>("File Extraction"))
            .AddSingleton<FileExtractor.FileExtractor>()
            .AddSingleton<ICoreDelegates, MockDelegates>()
            // .AddSingleton<TemporaryFileManager>()
            // .AddAllSingleton<IExtractor, SevenZipExtractor>()
            ;
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

