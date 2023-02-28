using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexusMods.App;
using NexusMods.DataModel;
using NexusMods.FileExtractor;
using NexusMods.Games.RedEngine;
using NexusMods.StandardGameLocators;
using NexusMods.StandardGameLocators.TestHelpers;
using Xunit.DependencyInjection;
using Xunit.DependencyInjection.Logging;

namespace NexusMods.UI.Tests;

public class Startup
{
    public void ConfigureServices(IServiceCollection container)
    {
        container.AddUniversalGameLocator<Cyberpunk2077>(new Version("1.61"));
        container.AddApp(addStandardGameLocators:false)
            .AddSingleton<AvaloniaApp>();
    }
    
    public void Configure(ILoggerFactory loggerFactory, ITestOutputHelperAccessor accessor) =>
        loggerFactory.AddProvider(new XunitTestOutputLoggerProvider(accessor, delegate { return true;}));
}

