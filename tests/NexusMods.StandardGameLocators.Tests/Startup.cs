using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.Common;
using NexusMods.Paths;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.StandardGameLocators.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container
            .AddStandardGameLocators(false)
            .AddSingleton<IFileSystem, InMemoryFileSystem>()
            .AddSingleton<TemporaryFileManager>()
            .AddStubbedGameLocators()
            .AddLogging(builder => builder.SetMinimumLevel(LogLevel.Debug))
            .Validate();
    }

    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true; }));
}
